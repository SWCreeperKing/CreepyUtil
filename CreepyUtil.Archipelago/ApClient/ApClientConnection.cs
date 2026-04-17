using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using CreepyUtil.Archipelago.Commands;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public bool IsConnected { get; private set; } = false;
    public int PlayerSlot { get; private set; }
    public string PlayerName { get; private set; }
    public string[] PlayerNames { get; private set; }
    public string[] PlayerGames { get; private set; }
    public ArchipelagoClientState[] PlayerStates { get; private set; }
    public IReadOnlyDictionary<string, object> SlotData { get; private set; } = null!;
    public List<string> MissingLocations { get; private set; } = [];
    public TwoWayLookup<long, string> Locations { get; private set; }
    public TwoWayLookup<long, string> Items { get; private set; }
    public TagManager Tags { get; private set; }

    public TimeSpan ServerTimeout
    {
        get => _ServerTimeout;
        set
        {
            _ServerTimeout = value;
            // todo: EWWW static
            ArchipelagoSession.ArchipelagoConnectionTimeoutInSeconds = (int)value.TotalSeconds;
        }
    }

    public bool ExcludeBouncedPacketsFromSelf = true;

    public bool HasGoaled
        => HasGoaledCached || Session?.DataStorage?.GetClientStatus() is ArchipelagoClientState.ClientGoal;

    private LoginInfo Info;
    private int[] PlayerSlotArr;
    private TimeSpan _ServerTimeout;

    public event Action<string>? OnDebugInfoMessage;
    public event Action<ApClient>? OnConnectionEvent;
    public event Action<int>? OnPlayerStateChanged;
    public event Action? OnConnectionLost;
    public event Action<string>? ItemsSentNotification;
    public event Action<ReadOnlyCollection<long>>? CheckedLocationsUpdated;
    public event ArchipelagoSocketHelperDelagates.ErrorReceivedHandler? OnConnectionErrorReceived;
    public event Action<Exception>? OnErrorReceived;
    public event Action<Hint[]>? HintsTrackedEvent; 

    public ApClient(TimeSpan? timeout = null) => ServerTimeout = timeout ?? new TimeSpan(0, 0, 10);

    public string[]? TryConnect(
        LoginInfo info, string gameName, ItemsHandlingFlags flags,
        Version? version = null, ArchipelagoTag[]? tags = null, bool requestSlotData = true
    )
    {
        Info = info;
        try
        {
            _ItemIdToName = [];
            _LocationIdToName = [];
            Session = ArchipelagoSessionFactory.CreateSession(Info.Address, Info.Port);
            (Socket = Session.Socket).ErrorReceived += (e, s) => OnConnectionErrorReceived?.Invoke(e, s);

            Tags = new TagManager(this, Session!, tags!);
            
            var result = Session.TryConnectAndLogin(
                gameName, Info.Slot, flags, version,
                Tags.GetTagsAsStrings(), null, Info.Password,
                requestSlotData
            );

            if (!result.Successful) return ((LoginFailure)result).Errors;
            PlayerSlot = Session.Players.ActivePlayer.Slot;
            PlayerSlotArr = [PlayerSlot];
            PlayerName = Session.Players.ActivePlayer.Name;
            PlayerNames = Session.Players.AllPlayers.Select(player => player.Name!).ToArray();
            PlayerGames = Session.Players.AllPlayers.Select(player => player.Game).ToArray();

            // ItemsReceivedCounter = GetFromStorage("items_received_counter", def: 0);
            // ItemsReceivedTracker = 0;
            ItemsReceivedCounter = 0;

            Session.DataStorage.TrackHints(hints
                    =>
                {
                    HintsTrackedEvent?.Invoke(hints);
                }
            );

            CommandHandler = new ApCommandHandler(this);
            Session.Socket.PacketReceived += OnPacketReceived;
            
            SetupDeathLink();

            Session.Locations.CheckedLocationsUpdated += locations => CheckedLocationsUpdated?.Invoke(locations);

            if (requestSlotData)
            {
                var slotDataTask = Session!.DataStorage.GetSlotDataAsync();
                slotDataTask.ContinueWith(slotData => SlotData = slotData.Result);
                slotDataTask.Wait();
            }

            GetLookups(PlayerGames[PlayerSlot], out var locations, out var items);
            Locations = locations;
            Items = items;

            MissingLocations = Session?.Locations.AllMissingLocations.Select(l => Locations[l]).ToList()!;

            IsConnected = true;
            OnConnectionEvent?.Invoke(this);
            return null;
        }
        catch (Exception e) { return [e.Message, e.StackTrace!]; }
    }

    public void GetLookups(string game, out TwoWayLookup<long, string> locations, out TwoWayLookup<long, string> items)
    {
        var itemReceivedResolver = (ReceivedItemsHelper)Session!.Items;
        var itemResolver = (ItemInfoResolver)itemReceivedResolver.itemInfoResolver;
        var lookup = (GameDataLookup)((DataPackageCache)itemResolver.cache).inMemoryCache[game];
        locations = lookup.Locations;
        items = lookup.Items;
    }

    /// <summary>
    /// Update connection status
    /// </summary>
    public void UpdateConnection()
    {
        if (!IsConnected || Session is null || Session.Socket.Connected || Session.Socket is null) return;
        IsConnected = false;
        OnConnectionLost!.Invoke();
    }

    public void TryDisconnect()
    {
        IsConnected = false;
        Session!.Socket.DisconnectAsync();
        Session = null;
    }
}