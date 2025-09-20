using System.Text;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Converters;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.Archipelago.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;

namespace CreepyUtil.Archipelago;

public class ApClient
{
    public bool IsConnected { get; private set; } = false;
    public ArchipelagoSession? Session { get; private set; }
    public IArchipelagoSocketHelper? Socket { get; private set; }
    public int PlayerSlot { get; private set; }
    public string PlayerName { get; private set; }
    public string[] PlayerNames { get; private set; }
    public string[] PlayerGames { get; private set; }
    public ArchipelagoClientState[] PlayerStates { get; private set; }
    public HashSet<Hint> Hints { get; set; } = [];
    public IReadOnlyDictionary<string, object> SlotData { get; private set; } = null!;
    public Dictionary<long, string> ItemIdToName { get; private set; } = [];
    public Dictionary<long, string> LocationIdToName { get; private set; } = [];
    public List<long> MissingLocations { get; private set; } = [];
    public TwoWayLookup<long, string> Locations { get; private set; }
    public TwoWayLookup<long, string> Items { get; private set; }

    public TimeSpan ServerTimeout = new(0, 0, 10);
    public bool HintsAwaitingUpdate { get; private set; } = false;

    public bool HasGoaled
        => HasGoaledCached || Session?.DataStorage?.GetClientStatus() is ArchipelagoClientState.ClientGoal;

    public bool HasPlayerListSetup = false;
    private bool HasGoaledCached = false;

    private LoginInfo Info;
    private Hint[] WaitingHints = [];
    private int[] PlayerSlotArr;
    private ApCommandHandler CommandHandler;

    public event Action<ApClient>? OnConnectionEvent;
    public event Action<int>? OnPlayerStateChanged;
    public event Action? OnConnectionLost;
    public event Action<HintPrintJsonPacket>? OnHintPrintJsonPacketReceived;
    public event Action<ChatPrintJsonPacket>? OnChatPrintPacketReceived;
    public event Action<BouncedPacket>? OnBouncedPacketReceived;
    public event Action<BouncedPacket>? OnDeathLinkPacketReceived;
    public event Action<PrintJsonPacket>? OnPrintJsonPacketReceived;
    public event Action<PrintJsonPacket>? OnServerMessagePacketReceived;
    public event Action<PrintJsonPacket>? OnItemLogPacketReceived;
    public event Action<string>? ItemsSentNotification;
    public event ArchipelagoSocketHelperDelagates.ErrorReceivedHandler? OnConnectionErrorReceived;

    public string[]? TryConnect(LoginInfo info, string gameName, ItemsHandlingFlags flags,
        Version? version = null,
        string[]? tags = null, bool requestSlotData = true)
    {
        Info = info;
        try
        {
            Session = ArchipelagoSessionFactory.CreateSession(Info.Address, Info.Port);
            (Socket = Session.Socket).ErrorReceived += (e, s) => OnConnectionErrorReceived?.Invoke(e, s);

            var result = Session.TryConnectAndLogin(gameName, Info.Slot, flags, version, tags, null, Info.Password,
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

            CommandHandler = new ApCommandHandler(this);
            Session.Socket.PacketReceived += packet =>
            {
                switch (packet)
                {
                    case HintPrintJsonPacket hintPacket:
                        OnHintPrintJsonPacketReceived?.Invoke(hintPacket);
                        break;
                    case ChatPrintJsonPacket message:
                        OnChatPrintPacketReceived?.Invoke(message);

                        if (message.Message.StartsWith($"@{PlayerName} "))
                        {
                            CommandHandler.RunCommand(message,
                                message.Message.Replace($"@{PlayerName} ", "").Split(' '));
                        }

                        break;
                    case BouncedPacket bouncedPacket:
                        OnBouncedPacketReceived?.Invoke(bouncedPacket);
                        if (bouncedPacket.Tags.Contains("DeathLink"))
                        {
                            OnDeathLinkPacketReceived?.Invoke(bouncedPacket);
                        }

                        break;
                    case PrintJsonPacket printPacket:
                        OnPrintJsonPacketReceived?.Invoke(printPacket);
                        if (printPacket.Data.Length == 1)
                        {
                            OnServerMessagePacketReceived?.Invoke(printPacket);
                            return;
                        }

                        if (printPacket.Data.Length < 2) break;
                        if (printPacket.Data[1].Text is " found their " or " sent ")
                        {
                            OnItemLogPacketReceived?.Invoke(printPacket);
                        }

                        break;
                }
            };

            MissingLocations = Session?.Locations.AllMissingLocations.ToList()!;
            if (requestSlotData)
            {
                SlotData = Session.DataStorage.GetSlotData();
            }

            var itemReceivedResolver = (ReceivedItemsHelper)Session!.Items;
            var itemResolver = (ItemInfoResolver)itemReceivedResolver.itemInfoResolver;
            var lookup = (GameDataLookup)((DataPackageCache)itemResolver.cache).inMemoryCache[PlayerGames[PlayerSlot]];
            Locations = lookup.Locations;
            Items = lookup.Items;

            OnConnectionEvent?.Invoke(this);
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
            Session?.DataStorage.TrackClientStatus(state =>
            {
                PlayerStates[i1] = state;
                OnPlayerStateChanged?.Invoke(i1);
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

    public void UpdateConnection()
    {
        if (!IsConnected || Session!.Socket.Connected) return;
        IsConnected = false;
        OnConnectionLost!.Invoke();
    }

    public IEnumerable<ItemInfo?> GetOutstandingItems()
    {
        while (Session!.Items.Any()) yield return Session.Items.DequeueItem();
    }

    public bool SendLocation(string id) => SendLocation(Locations[id]);
    public bool SendLocations(string[] id) => SendLocations(id.Select(loc => Locations[loc]).ToArray());

    private bool SendLocation(long id)
    {
        if (!MissingLocations.Contains(id)) return true;
        return IsConnected && new Task(() =>
        {
            if (MissingLocations.Count == 0) return;
            Session?.Locations.CompleteLocationChecks(id);
            MissingLocations.Remove(id);
            ItemsSentNotification?.Invoke(Locations[id]);
        }).RunWithTimeout(ServerTimeout);
    }

    private bool SendLocations(params long[] ids)
    {
        ids = ids.Where(id => MissingLocations.Contains(id)).ToArray();
        if (ids.Length == 0) return true;
        return IsConnected && new Task(() =>
        {
            if (MissingLocations.Count == 0) return;
            Session?.Locations.CompleteLocationChecks(ids);
            foreach (var loc in ids)
            {
                MissingLocations.Remove(loc);
                ItemsSentNotification?.Invoke(Locations[loc]);
            }
        }).RunWithTimeout(ServerTimeout);
    }

    public void SendDeathLink(string cause)
    {
        new Task(() =>
            {
                lock (Session!)
                {
                    Session?.Socket.SendPacketAsync(new BouncePacket
                    {
                        Tags = ["DeathLink"],
                        Data = new Dictionary<string, JToken>
                        {
                            { "time", DateTime.UtcNow.ToUnixTimeStamp() },
                            { "source", PlayerName },
                            { "cause", cause }
                        }
                    });
                }
            })
           .RunWithTimeout(ServerTimeout);
    }

    public void UpdateHint(int slot, long location, HintStatus priority)
    {
        Session?.Socket.SendPacketAsync(new UpdateHintPacket
                 {
                     Player = slot,
                     Location = location,
                     Status = priority
                 })
                .GetAwaiter()
                .GetResult();
    }

    public string ItemIdToItemName(long id, int playerSlot)
    {
        if (!ItemIdToName.TryGetValue(id, out var itemName))
        {
            itemName = ItemIdToName[id] = Session!.Items.GetItemName(id, PlayerGames[playerSlot]);
        }

        return itemName;
    }

    public string LocationIdToLocationName(long id, int playerSlot)
    {
        if (!LocationIdToName.TryGetValue(id, out var location))
        {
            location = LocationIdToName[id] = Session!.Locations.GetLocationNameFromId(id, PlayerGames[playerSlot]);
        }

        return location;
    }

    public void Goal()
    {
        if (HasGoaledCached) return;
        Session?.SetGoalAchieved();
        HasGoaledCached = true;
    }

    public void TryDisconnect()
    {
        IsConnected = false;
        new Task(() =>
            {
                lock (Session!)
                {
                    Session.Socket.DisconnectAsync();
                }
            })
           .RunWithTimeout(ServerTimeout);
        Session = null;
    }

    public T? GetFromStorage<T>(string key, Scope scope = Scope.Slot, T? def = default)
    {
        T? data;
        try
        {
            data = JsonConvert.DeserializeObject<T>(Session!.DataStorage[scope, key].To<string>())!;
        }
        catch
        {
            //ignore
            data = def;
        }

        return data;
    }

    public void SendToStorage<T>(string key, T data, Scope scope = Scope.Slot)
        => Session!.DataStorage[scope, key] = JsonConvert.SerializeObject(data);

    public bool Say(string message)
    {
        return IsConnected && new Task(() =>
        {
            lock (Session!)
            {
                Session.Say(message);
            }
        }).RunWithTimeout(ServerTimeout);
    }

    public void RegisterCommand(IApCommandInterface command) => CommandHandler.RegisterCommand(command);
    public void DeregisterCommand(IApCommandInterface command) => CommandHandler.DeregisterCommand(command);
}

public static class Helper
{
    public static bool RunWithTimeout(this Task task, TimeSpan timeout)
    {
        task.Start();
        return Task.WaitAny([task], timeout) != -1;
    }

    public static int SortNumber(this HintStatus status)
    {
        return status switch
        {
            HintStatus.Found => 0,
            HintStatus.NoPriority => 2,
            HintStatus.Avoid => 4,
            HintStatus.Priority => 1,
            _ => 3,
        };
    }

    public static int SortNumber(this ItemFlags item)
    {
        if (item.HasFlag(Advancement)) return 0;
        if (item.HasFlag(Trap)) return 10;
        return item.HasFlag(NeverExclude) ? 1 : 2;
    }

    public static string GetAsTime(this double time, bool staticSecEnding = true)
    {
        var sec = time % 60;
        time = Math.Floor(time / 60f);
        var min = time % 60;
        time = Math.Floor(time / 60f);
        var hour = time % 24;
        var days = Math.Floor(time / 24f);

        StringBuilder sb = new();
        if (days > 0) sb.Append(days).Append("d ");
        if (hour > 0) sb.Append(hour).Append("hr ");
        if (min > 0) sb.Append(min).Append("m ");
        switch (sec)
        {
            case > 0 when staticSecEnding:
                sb.Append($"{sec:#0.00}").Append("s ");
                break;
            case > 0:
                sb.Append($"{sec:#0.##}").Append("s ");
                break;
        }

        if (sb.Length == 0) sb.Append("0s ");
        return sb.ToString().TrimEnd();
    }

    public static HashSet<Hint> OrderHints(this IEnumerable<Hint> hints, int playerCount, IEnumerable<int> PlayerSlots)
    {
        return hints
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

    public static T? SafeTo<T>(this DataStorageElement? element, T? def = default)
    {
        try
        {
            return element!.To<T>() ?? def;
        }
        catch
        {
            return def;
        }
    }
}