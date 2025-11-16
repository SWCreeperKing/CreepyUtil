using NLua;

namespace CreepyUtil.Archipelago.PackReader.Bridge;

public abstract class Archipelago
{
    public abstract int PlayerNumber { get; }
    public abstract int TeamNumber { get; }
    public abstract long[] CheckedLocations { get; }
    public abstract long[] MissingLocations { get; }

    public event Action<Dictionary<string, object>>? OnNewConnection;
    public event Action<int, long, string, int>? OnItemReceived;
    public event Action<long, string>? OnLocationChecked;
    public event Action<long, string, long, string, int>? OnLocationScouted;
    public event Action<Dictionary<string, object>>? OnPacketBounced;
    public event Action<string, object>? OnGetRetrieved;
    public event Action<string, object, object>? OnSetReplyStorageChanged;

    public void AddClearHandler(string _, LuaFunction callback) => OnNewConnection += dict => callback.Call(dict);

    public void AddItemHandler(string _, LuaFunction callback)
        => OnItemReceived += (index, itemId, itemName, playerNumber)
            => callback.Call(index, itemId, itemName, playerNumber);

    public void AddLocationHandler(string _, LuaFunction callback)
        => OnLocationChecked += (locationId, locationName) =>
            callback.Call(locationId, locationName);

    public void AddScoutHandler(string _, LuaFunction callback)
        => OnLocationScouted += (locationId, locationName, itemId, itemName, playerNumber)
            => callback.Call(locationId, locationName, itemId, itemName, playerNumber);

    public void AddBouncedHandler(string _, LuaFunction callback)
        => OnPacketBounced += bouncePacket => callback.Call(bouncePacket);

    public void AddRetrievedHandler(string _, LuaFunction callback)
        => OnGetRetrieved += (key, value) => callback.Call(key, value);

    public void AddSetReplyHandler(string _, LuaFunction callback)
        => OnSetReplyStorageChanged += (key, value, oldValue) => callback.Call(key, value, oldValue);

    /// <summary>
    /// get from data storage
    /// </summary>
    /// <param name="keys">keys to get</param>
    /// <returns>true on success</returns>
    public abstract bool Get(string[] keys);

    /// <summary>
    /// set keys to get notified when changed for data storage
    /// </summary>
    /// <param name="keys">keys to watch</param>
    /// <returns>true on success</returns>
    public abstract bool SetNotify(string[] keys);

    // ignored, will not support apmanual
    public bool LocationChecks(long[] locations) => false;

    // ignored, will not support apmanual/aphintgame
    public bool LocationScouts(long[] locations, int sendAsHint) => false;

    // ignored, will not support apmanual
    public bool StatusUpdate(object status) => false;

    public abstract string GetPlayerAlias(int slot);
    public abstract string GetPlayerGame(int slot);
    public abstract string GetItemName(long id, string game);
    public abstract string GetLocationName(long id, string game);
    
    [LuaHide] public void RunOnConnect(Dictionary<string, object> slotData) => OnNewConnection?.Invoke(slotData);

    [LuaHide]
    public void RunOnItemReceived(int index, long itemId, string itemName, int playerNumber)
        => OnItemReceived?.Invoke(index, itemId, itemName, playerNumber);

    [LuaHide]
    public void RunOnLocationChecked(long locationId, string locationName)
        => OnLocationChecked?.Invoke(locationId, locationName);

    [LuaHide]
    public void RunOnLocationScouted(long locationId, string locationName, long itemId, string itemName,
        int playerNumber)
        => OnLocationScouted?.Invoke(locationId, locationName, itemId, itemName, playerNumber);

    [LuaHide] public void RunOnPacketBounced(Dictionary<string, object> data) => OnPacketBounced?.Invoke(data);
    [LuaHide] public void RunOnGetRetrieved(string key, object value) => OnGetRetrieved?.Invoke(key, value);

    [LuaHide]
    public void RunOnSetReplyStorageChanged(string key, object value, object oldValue)
        => OnSetReplyStorageChanged?.Invoke(key, value, oldValue);
}