using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;

namespace CreepyUtil.Archipelago;

public class ApClient
{
    public bool IsConnected { get; private set; } = false;
    public long UUID { get; private set; }
    public ArchipelagoSession Session { get; private set; }
    public int PlayerSlot { get; private set; }
    public string PlayerName { get; private set; }
    public string[] PlayerNames { get; private set; }
    public string[] PlayerGames { get; private set; }
    public ArchipelagoClientState[] PlayerStates { get; private set; }
    public HashSet<Hint> Hints { get; set; } = [];
    public IReadOnlyDictionary<string, object> SlotData { get; private set; } = null!;
    public Dictionary<long, string> ItemIdToName { get; private set; } = [];
    public Dictionary<long, string> LocationIdToName { get; private set; } = [];
    public Dictionary<long, ScoutedItemInfo> MissingLocations { get; private set; } = [];
    public bool HintsAwaitingUpdate { get; private set; } = false;
    public bool HasPlayerListSetup = false;

    private LoginInfo Info;
    private long gameUUID;
    private Hint[] WaitingHints = [];
    private int[] PlayerSlotArr;

    public event EventHandler<ApClient>? OnConnectionEvent;
    public event EventHandler<int>? OnPlayerStateChanged;

    public string[]? TryConnect(LoginInfo info, long gameUUID, string gameName, ItemsHandlingFlags flags,
        Version? version = null,
        string[]? tags = null,
        string? uuid = null, bool requestSlotData = true)
    {
        UUID = gameUUID;
        Info = info;
        try
        {
            Session = ArchipelagoSessionFactory.CreateSession(Info.Address, Info.Port);

            var result = Session.TryConnectAndLogin(gameName, Info.Slot, flags, version, tags, uuid, Info.Password,
                requestSlotData);

            if (!result.Successful) return ((LoginFailure)result).Errors;
            PlayerSlot = Session.Players.ActivePlayer.Slot;
            PlayerSlotArr = [PlayerSlot];
            PlayerName = Session.Players.ActivePlayer.Name;
            PlayerNames = Session.Players.AllPlayers.Select(player => player.Name!).ToArray();
            PlayerGames = Session.Players.AllPlayers.Select(player => player.Game).ToArray();

            Session.DataStorage.TrackHints(hints
                =>
            {
                WaitingHints = hints;
                HintsAwaitingUpdate = true;
            });

            ReloadLocations();
            if (requestSlotData)
            {
                SlotData = Session.DataStorage.GetSlotData();
            }

            OnConnectionEvent?.Invoke(null, this);
            IsConnected = true;
            return null;
        }
        catch (Exception e)
        {
            return [e.Message, e.StackTrace!];
        }
    }

    public void SetupPlayerList()
    {
        if (HasPlayerListSetup) return;
        HasPlayerListSetup = true;
        PlayerStates = PlayerNames.Select(_ => ArchipelagoClientState.ClientUnknown).ToArray();

        for (var i = 0; i < PlayerNames.Length; i++)
        {
            var i1 = i;
            Session.DataStorage.TrackClientStatus(state =>
            {
                PlayerStates[i1] = state;
                OnPlayerStateChanged?.Invoke(null, i1);
            }, true, i1);
        }
    }

    public bool PushUpdatedVariables(bool updateHintArr, out Hint[] hints)
    {
        var hasAnythingChanged = false;
        if (HintsAwaitingUpdate)
        {
            if (updateHintArr)
            {
                Hints = WaitingHints.OrderHints(PlayerNames.Length, PlayerSlotArr);
            }

            hasAnythingChanged = true;
            HintsAwaitingUpdate = false;
        }

        hints = WaitingHints;
        return hasAnythingChanged;
    }

    public IEnumerable<ItemInfo?> GetOutstandingItems()
    {
        while (Session.Items.Any())
        {
            yield return Session.Items.DequeueItem();
        }
    }

    public void SendLocation(long id)
    {
        if (MissingLocations.Count == 0) return;
        var loc = MissingLocations[id];
        Session.Locations.CompleteLocationChecks(loc.LocationId);
        MissingLocations.Remove(id);
    }

    public void ReloadLocations()
    {
        var missing = Session.Locations.AllMissingLocations.ToArray();
        var scoutedLocations = Session.Locations.ScoutLocationsAsync(missing).Result!;
        MissingLocations.Clear();

        foreach (var (id, location) in scoutedLocations.OrderBy(loc => loc.Key))
        {
            MissingLocations[id - UUID] = location;
        }
    }

    public void SendDeathLink(string cause)
        => Session.Socket.SendPacketAsync(new BouncePacket
                   {
                       Tags = new List<string> { "DeathLink" },
                       Data = new Dictionary<string, JToken>
                       {
                           { "time", DateTime.UtcNow.ToUnixTimeStamp() },
                           { "source", PlayerName },
                           { "cause", cause }
                       }
                   })
                  .GetAwaiter()
                  .GetResult();

    public void UpdateHint(int slot, long location, HintStatus priority)
        => Session.Socket.SendPacketAsync(new UpdateHintPacket
                   {
                       Player = slot,
                       Location = location,
                       Status = priority
                   })
                  .GetAwaiter()
                  .GetResult();

    public string ItemIdToItemName(long id, int playerSlot)
    {
        if (!ItemIdToName.TryGetValue(id, out var itemName))
        {
            itemName = ItemIdToName[id] = Session.Items.GetItemName(id, PlayerGames[playerSlot]);
        }

        return itemName;
    }

    public string LocationIdToLocationName(long id, int playerSlot)
    {
        if (!LocationIdToName.TryGetValue(id, out var location))
        {
            location = LocationIdToName[id] = Session.Locations.GetLocationNameFromId(id, PlayerGames[playerSlot]);
        }

        return location;
    }

    public void SendMessage(string message) => Session.Say(message);
    public void Goal() => Session.SetGoalAchieved();

    public void TryDisconnect()
    {
        try
        {
            Session.Socket.DisconnectAsync().GetAwaiter().GetResult();
            Session = null!;
        }
        catch
        {
            //ignored
        }
    }

    public DataStorageElement? this[string key, Scope scope = Scope.Slot]
    {
        get => Session.DataStorage[scope, key];
        set => Session.DataStorage[scope, key] = value;
    }

    public void Say(string message) { Session.Say(message); }
}

public static class Helper
{
    public static int SortNumber(this HintStatus status)
        => status switch
        {
            HintStatus.Found => 0,
            HintStatus.Unspecified => 3,
            HintStatus.NoPriority => 2,
            HintStatus.Avoid => 4,
            HintStatus.Priority => 1,
        };

    public static int SortNumber(this ItemFlags item)
    {
        if (item.HasFlag(Advancement)) return 0;
        if (item.HasFlag(Trap)) return 10;
        return item.HasFlag(NeverExclude) ? 1 : 2;
    }

    public static string GetAsTime(this double time)
    {
        var sec = time % 60;
        time = (float)Math.Floor(time / 60f);
        var min = time % 60;
        time = (float)Math.Floor(time / 60f);
        var hour = time % 24;
        var days = (float)Math.Floor(time / 24f);

        StringBuilder sb = new();
        if (days > 0) sb.Append(days).Append("d ");
        if (hour > 0) sb.Append(hour).Append("hr ");
        if (min > 0) sb.Append(min).Append("m ");
        if (sec > 0) sb.Append($"{sec:#0.00}").Append("s ");
        return sb.ToString().TrimEnd();
    }

    public static HashSet<Hint> OrderHints(this IEnumerable<Hint> hints, int playerCount, IEnumerable<int> PlayerSlots)
        => hints
          .OrderBy(hint => hint.Status.SortNumber())
          .ThenBy(hint => hint.ItemFlags.SortNumber())
          .ThenBy(hint
               => PlayerSlots.Contains(hint.ReceivingPlayer)
                   ? playerCount + 1
                   : hint.ReceivingPlayer)
          .ThenBy(hint
               => PlayerSlots.Contains(hint.FindingPlayer)
                   ? playerCount + 1
                   : hint.FindingPlayer)
          .ThenBy(hint => hint.LocationId)
          .ToHashSet();
}