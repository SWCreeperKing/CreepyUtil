using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public PlayerInfo[] AllPlayers => Session?.Players.AllPlayers.ToArray()!;
    public IRoomStateHelper RoomState => Session?.RoomState!;
    public bool HasPlayerListSetup;

    private IArchipelagoSocketHelper? Socket;
    private ArchipelagoSession? Session;

    public string? Seed => Session?.RoomState.Seed;
    public int LocationCount => (int)Session?.Locations.AllLocations.Count!;
    public int LocationsCheckedCount => (int)Session?.Locations.AllLocationsChecked.Count!;
    public string[] LocationsChecked => Session?.Locations.AllLocationsChecked.Select(l => Locations[l]).ToArray()!;

    public ReadOnlyCollection<long> MissingLocations => Session is null || !IsConnected ? new ReadOnlyCollection<long>([])
        : Session!.Locations.AllMissingLocations;

    private Dictionary<string, Dictionary<long, string>> _ItemIdToName = [];
    private Dictionary<string, Dictionary<long, string>> _LocationIdToName = [];

    public bool IsMissingLocation(string loc) => MissingLocations.Contains(Locations[loc]);
    
    public bool SendLocation(string id)
    {
        try
        {
            if (!IsMissingLocation(id)) return true;
            return IsConnected && new Task(() =>
                {
                    if (MissingLocations.Count == 0) return;
                    Session?.Locations.CompleteLocationChecks(Locations[id]);
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
            ids = [.. ids.Where(IsMissingLocation)];
            if (ids.Length == 0) return true;
            return IsConnected && new Task(() =>
                {
                    if (MissingLocations.Count == 0) return;
                    Session?.Locations.CompleteLocationChecks([.. ids.Select(id => Locations[id])]);
                    foreach (var loc in ids) ItemsSentNotification?.Invoke(loc);
                }
            ).RunWithTimeout(ServerTimeout, OnErrorReceived);
        }
        catch (Exception e) { OnErrorReceived?.Invoke(e); }
        return false;
    }

    public ScoutedItemInfo? ScoutLocation(string id, HintCreationPolicy hintCreationPolicy = HintCreationPolicy.None)
    {
        if (!Locations.TryGetValue(id, out var location))
        {
            OnErrorReceived?.Invoke(new ArgumentException($"Location: [{id}] does not exist to scout"));
            return null;
        }

        var items = Session?.Locations.ScoutLocationsAsync(hintCreationPolicy, location).GetAwaiter().GetResult();
        return items?[location];
    }

    public ScoutedItemInfo?[]? ScoutLocations(string[] ids,
        HintCreationPolicy hintCreationPolicy = HintCreationPolicy.None)
    {
        var locationsNotFound = ids.Where(id => Locations.All(kv => kv.Key != id)).ToArray();
        if (locationsNotFound.Length != 0)
        {
            OnErrorReceived?.Invoke(
                new ArgumentException($"Locations: [{string.Join(", ", locationsNotFound)}] does not exist to scout")
            );
        }

        var locations = ids.Where(id => Locations.Any(kv => kv.Key == id)).Select(id => Locations[id]).ToArray();
        var items = Session?.Locations.ScoutLocationsAsync(hintCreationPolicy, locations).GetAwaiter().GetResult();
        return [.. locations.Where(loc => items is not null && items.ContainsKey(loc)).Select(loc => items![loc])];
    }

    public void CreateHints(HintStatus status = HintStatus.Unspecified, params string[] locations)
    {
        Session?.Hints.CreateHints(status, [.. locations.Select(id => Locations[id])]);
    }

    public void CreateHints(HintStatus status = HintStatus.Unspecified, params long[] locations)
    {
        Session?.Hints.CreateHints(status, locations);
    }

    public void SetupPlayerList()
    {
        if (HasPlayerListSetup) return;
        HasPlayerListSetup = true;
        PlayerStates = [.. PlayerNames.Select(_ => ArchipelagoClientState.ClientUnknown)];

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

    public string? LocationIdToLocationName(long id, int playerSlot)
    {
        var game = PlayerGames[playerSlot];
        if (!_LocationIdToName.TryGetValue(game, out var dict))
        {
            _LocationIdToName[game] = dict = new Dictionary<long, string>();
        }

        if (!dict.TryGetValue(id, out var location))
        {
            var loc = Session!.Locations.GetLocationNameFromId(id, PlayerGames[playerSlot]);
            if (loc is null) return null;
            location = _LocationIdToName[game][id] = loc;
        }

        return location;
    }

    public string? GetAlias(int slot) => Session?.Players.GetPlayerAliasAndName(slot);
}