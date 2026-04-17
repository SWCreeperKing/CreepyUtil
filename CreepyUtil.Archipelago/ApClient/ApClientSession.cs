using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public PlayerInfo[] AllPlayers => Session?.Players.AllPlayers.ToArray()!;
    public IRoomStateHelper RoomState => Session?.RoomState!;

    public bool HasPlayerListSetup = false;

    private IArchipelagoSocketHelper? Socket;
    private ArchipelagoSession? Session;
    private int ItemsReceivedCounter;
    private int ItemsReceivedTracker;

    public string? Seed => Session?.RoomState.Seed;
    public int LocationCount => (int)Session?.Locations.AllLocations.Count!;
    public int LocationsCheckedCount => (int)Session?.Locations.AllLocationsChecked.Count!;
    public string[] LocationsChecked => Session?.Locations.AllLocationsChecked.Select(l => Locations[l]).ToArray()!;

    private Dictionary<string, Dictionary<long, string>> _ItemIdToName = [];
    private Dictionary<string, Dictionary<long, string>> _LocationIdToName = [];

    public ItemInfo[] GetOutstandingItems()
    {
        if (ItemsReceivedCounter >= Session!.Items.AllItemsReceived.Count) return [];
        var arr = Session!.Items.AllItemsReceived.Skip(ItemsReceivedCounter).ToArray();
        ItemsReceivedCounter += arr.Length;

        // if (newOnly)
        // {
        //     var delta = ItemsReceivedCounter - ItemsReceivedTracker;
        //     if (delta < 0) return arr;
        //     
        // }

        return arr;
    }

    public bool SendLocation(string id)
    {
        try
        {
            if (!MissingLocations.Contains(id)) return true;
            return IsConnected && new Task(() =>
                {
                    if (MissingLocations.Count == 0) return;
                    Session?.Locations.CompleteLocationChecks(Locations[id]);
                    MissingLocations.Remove(id);
                    ItemsSentNotification?.Invoke(id);
                }
            ).RunWithTimeout(ServerTimeout, OnErrorReceived);
        }
        catch (Exception e) { OnErrorReceived?.Invoke(e); }
        return false;
    }

    public bool SendLocations(string[] ids)
    {
        try
        {
            ids = ids.Where(id => MissingLocations.Contains(id)).ToArray();
            if (ids.Length == 0) return true;
            return IsConnected && new Task(() =>
                {
                    if (MissingLocations.Count == 0) return;
                    Session?.Locations.CompleteLocationChecks(ids.Select(id => Locations[id]).ToArray());
                    foreach (var loc in ids)
                    {
                        MissingLocations.Remove(loc);
                        ItemsSentNotification?.Invoke(loc);
                    }
                }
            ).RunWithTimeout(ServerTimeout, OnErrorReceived);
        }
        catch (Exception e) { OnErrorReceived?.Invoke(e); }
        return false;
    }
    
    public ScoutedItemInfo? ScoutLocation(string id, HintCreationPolicy hintCreationPolicy = HintCreationPolicy.None)
    {
        var location = Locations[id];
        var items = Session?.Locations.ScoutLocationsAsync(hintCreationPolicy, location).GetAwaiter().GetResult();
        return items?[location];
    }
    
    public ScoutedItemInfo?[] ScoutLocations(string[] ids, HintCreationPolicy hintCreationPolicy = HintCreationPolicy.None)
    {
        var locations = ids.Select(id => Locations[id]).ToArray();
        var items = Session?.Locations.ScoutLocationsAsync(hintCreationPolicy, locations).GetAwaiter().GetResult();
        return locations.Where(loc => items is not null && items.ContainsKey(loc)).Select(loc => items![loc]).ToArray();
    }

    public void CreateHints(HintStatus status = HintStatus.Unspecified, params string[] locations)
    {
        Session?.Hints.CreateHints(status, locations.Select(id => Locations[id]).ToArray());
    }
    
    public void CreateHints(HintStatus status = HintStatus.Unspecified, params long[] locations)
    {
        Session?.Hints.CreateHints(status, locations);
    }

    public void SetupPlayerList()
    {
        if (HasPlayerListSetup) return;
        HasPlayerListSetup = true;
        PlayerStates = PlayerNames.Select(_ => ArchipelagoClientState.ClientUnknown).ToArray();
        
        for (var i = 0; i < PlayerNames.Length; i++)
        {
            var i1 = i;
            Session?.DataStorage.TrackClientStatus(
                state =>
                {
                    PlayerStates[i1] = state;
                    OnPlayerStateChanged?.Invoke(i1);
                }, true, i1
            );
        }
    }

    public string ItemIdToItemName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!_ItemIdToName.TryGetValue(game, out var dict))
        {
            _ItemIdToName[game] = dict = new Dictionary<long, string>();
        }

        if (!dict.TryGetValue(id, out var itemName))
        {
            itemName = _ItemIdToName[game][id] = Session!.Items.GetItemName(id, PlayerGames[playerSlot]);
        }

        return itemName;
    }

    public string LocationIdToLocationName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!_LocationIdToName.TryGetValue(game, out var dict))
        {
            _LocationIdToName[game] = dict = new Dictionary<long, string>();
        }

        if (!dict.TryGetValue(id, out var location))
        {
            location = _LocationIdToName[game][id]
                = Session!.Locations.GetLocationNameFromId(id, PlayerGames[playerSlot]);
        }

        return location;
    }

    public string? GetAlias(int slot) => Session?.Players.GetPlayerAliasAndName(slot);

    public T? GetFromStorage<T>(string key, Scope scope = Scope.Slot, T? def = default)
    {
        T? data;
        try { data = JsonConvert.DeserializeObject<T>(Session!.DataStorage[scope, key].To<string>())!; }
        catch
        {
            //ignore
            data = def;
        }

        return data;
    }

    public void SendToStorage<T>(string key, T data, Scope scope = Scope.Slot)
        => Session!.DataStorage[scope, key] = JsonConvert.SerializeObject(data);
}