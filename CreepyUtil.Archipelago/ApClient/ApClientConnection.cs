using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.DataPackage;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
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
    public bool HintsAwaitingUpdate { get; private set; } = false;

    public TimeSpan ServerTimeout
    {
        get => _ServerTimeout;
        set
        {
            _ServerTimeout = value;
            // todo: EWWW static
            ArchipelagoSession.ArchipelagoConnectionTimeoutInSeconds = (int) value.TotalSeconds;
        }
    }

    public bool ExcludeBouncedPacketsFromSelf = true;

    public bool HasGoaled
        => HasGoaledCached || Session?.DataStorage?.GetClientStatus() is ArchipelagoClientState.ClientGoal;

    private bool HasGoaledCached = false;
    private LoginInfo Info;
    private int[] PlayerSlotArr;
    private TimeSpan _ServerTimeout;

    public event Action<string>? OnDebugInfoMessage;
    public event Action<ApClient>? OnConnectionEvent;
    public event Action<int>? OnPlayerStateChanged;
    public event Action? OnConnectionLost;
    public event Action<HintPrintJsonPacket>? OnHintPrintJsonPacketReceived;
    public event Action<ChatPrintJsonPacket>? OnChatPrintPacketReceived;
    public event Action<BouncedPacket>? OnBouncedPacketReceived;
    public event Action<PrintJsonPacket>? OnPrintJsonPacketReceived;
    public event Action<PrintJsonPacket>? OnServerMessagePacketReceived;
    public event Action<PrintJsonPacket>? OnItemLogPacketReceived;
    public event Action<string>? ItemsSentNotification;
    public event Action<ReadOnlyCollection<long>>? CheckedLocationsUpdated;
    public event ArchipelagoSocketHelperDelagates.ErrorReceivedHandler? OnConnectionErrorReceived;

    public ApClient(TimeSpan? timeout = null) => ServerTimeout = timeout ?? new TimeSpan(0, 0, 10);

    public string[]? TryConnect(LoginInfo info, string gameName, ItemsHandlingFlags flags,
        Version? version = null, ArchipelagoTag[]? tags = null, bool requestSlotData = true)
    {
        Info = info;
        try
        {
            _ItemIdToName = [];
            _LocationIdToName = [];
            Session = ArchipelagoSessionFactory.CreateSession(Info.Address, Info.Port);
            (Socket = Session.Socket).ErrorReceived += (e, s) => OnConnectionErrorReceived?.Invoke(e, s);

            var result = Session.TryConnectAndLogin(gameName, Info.Slot, flags, version,
                tags?.Select(tag => tag.StringTag()).ToArray(), null, Info.Password,
                requestSlotData);

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
                            if (!Tags[ArchipelagoTag.DeathLink]) return;
                            var source = (string)bouncedPacket.Data["source"]!;
                            if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;
                            var message = bouncedPacket.Data.TryGetValue("cause", out var cause)
                                ? (string)cause!
                                : "Died a generic (unknown) death";
                            OnDeathLinkPacketReceived?.Invoke(source, message);
                        }

                        if (bouncedPacket.Tags.Contains("TrapLink"))
                        {
                            if (!Tags[ArchipelagoTag.TrapLink]) return;
                            var source = (string)bouncedPacket.Data["source"]!;
                            if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;
                            var trap = (string)bouncedPacket.Data["trap_name"]!;

                            if (TrapLink.ContainsKey(trap))
                            {
                                TrapLink[trap](source);
                            }
                            else
                            {
                                OnUnregisteredTrapLinkReceived?.Invoke(source, trap);
                            }
                        }

                        if (bouncedPacket.Tags.Contains("RingLink"))
                        {
                            if (!Tags[ArchipelagoTag.RingLink]) return;
                            var source = PlayerNames[(int)bouncedPacket.Data["source"]!];
                            if (source == PlayerName && ExcludeBouncedPacketsFromSelf) return;
                            var amount = (int)bouncedPacket.Data["amount"]!;
                            OnRingLinkPacketReceived?.Invoke(source, amount);
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

            Session.Locations.CheckedLocationsUpdated += locations => CheckedLocationsUpdated?.Invoke(locations);

            if (requestSlotData) {
                var slotDataTask = Session!.DataStorage.GetSlotDataAsync();
                slotDataTask.ContinueWith(slotData => SlotData = slotData.Result);
                slotDataTask.Wait();
            }

            GetLookups(PlayerGames[PlayerSlot], out var locations, out var items);
            Locations = locations;
            Items = items;

            MissingLocations = Session?.Locations.AllMissingLocations.Select(l => Locations[l]).ToList()!;

            OnConnectionEvent?.Invoke(this);
            IsConnected = true;
            Tags = new TagManager(Session!);
            return null;
        }
        catch (Exception e)
        {
            return [e.Message, e.StackTrace!];
        }
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

    public void Goal()
    {
        if (HasGoaledCached) return;
        Session?.SetGoalAchieved();
        HasGoaledCached = true;
    }
}