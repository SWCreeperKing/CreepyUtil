using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public Dictionary<long, string> ItemIdToName { get; private set; } = [];
    public Dictionary<long, string> LocationIdToName { get; private set; } = [];
    
    public bool HasPlayerListSetup = false;
    
    private IArchipelagoSocketHelper? Socket;
    private ArchipelagoSession? Session;

    public IEnumerable<ItemInfo?> GetOutstandingItems()
    {
        while (Session!.Items.Any()) yield return Session.Items.DequeueItem();
    }

    public bool SendLocation(string id){
        if (!MissingLocations.Contains(id)) return true;
        return IsConnected && new Task(() =>
        {
            if (MissingLocations.Count == 0) return;
            Session?.Locations.CompleteLocationChecks(Locations[id]);
            MissingLocations.Remove(id);
            ItemsSentNotification?.Invoke(id);
        }).RunWithTimeout(ServerTimeout);
    }
    
    public bool SendLocations(string[] ids)
    {
        ids = ids.Where(id => MissingLocations.Contains(id)).ToArray();
        if (ids.Length == 0) return true;
        return IsConnected && new Task(() =>
        {
            if (MissingLocations.Count == 0) return;
            Session?.Locations.CompleteLocationChecks(ids.Select<string, long>(id => Locations[id]).ToArray());
            foreach (var loc in ids)
            {
                MissingLocations.Remove(loc);
                ItemsSentNotification?.Invoke(loc);
            }
        }).RunWithTimeout(ServerTimeout);
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
}