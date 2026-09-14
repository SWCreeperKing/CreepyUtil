using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public event Action<Exception>? OnDataStorageListenerError;
    private Dictionary<int, DataStorageHelper.DataStorageUpdatedHandler> CoreDsListeners = [];
    private Dictionary<int, Dictionary<string, DataStorageHelper.DataStorageUpdatedHandler>> DsListeners = [];

    public bool ContainsDataStorageListener(string key, string functionId, Scope scope = Scope.Global)
        => ContainsDataStorageListener(key, functionId, (int)scope);

    public bool ContainsDataStorageListener(string key, string functionId, int scope = -1)
    {
        var id = (key, scope).GetHashCode();
        if (!CoreDsListeners.ContainsKey(id)) return false;
        return DsListeners.TryGetValue(id, out var s) && s.ContainsKey(functionId);
    }

    public void AddDataStorageListener(string key, string functionId,
        DataStorageHelper.DataStorageUpdatedHandler action, Scope scope = Scope.Global)
        => AddDataStorageListener(key, functionId, action, (int)scope);

    public void AddDataStorageListener(string key, string functionId,
        DataStorageHelper.DataStorageUpdatedHandler action, int scope = -1)
    {
        try
        {
            if (!IsConnected) return;
            var id = (key, scope).GetHashCode();
            if (!DsListeners.ContainsKey(id)) DsListeners[id] = [];
            if (DsListeners[id].ContainsKey(functionId))
            {
                OnDataStorageListenerError?.Invoke(
                    new ArgumentException($"Key Function [{functionId}] already assigned to [{key}]")
                );
                return;
            }
            DsListeners[id][functionId] = action;

            if (CoreDsListeners.ContainsKey(id)) return;
            DataStorageHelper.DataStorageUpdatedHandler listener = (o, n, d) => OnValueChanged(id, o, n, d);
            GetDataStorageElement(key, scope).OnValueChanged += listener;
            CoreDsListeners[id] = listener;
        }
        catch (Exception e) { OnDataStorageListenerError?.Invoke(e); }
    }

    public void RemoveDataStorageListeners(string key, string functionId, Scope scope = Scope.Global)
        => RemoveDataStorageListeners(key, functionId, (int)scope);

    public void RemoveDataStorageListeners(string key, string functionId, int scope = -1)
    {
        try
        {
            if (!IsConnected) return;
            var id = (key, scope).GetHashCode();
            if (!DsListeners.TryGetValue(id, out var dsListeners)) return;
            if (!dsListeners.Remove(functionId)) return;
            if (DsListeners[id].Count != 0) return;
            if (!CoreDsListeners.TryGetValue(id, out var listener)) return;

            GetDataStorageElement(key, scope).OnValueChanged -= listener;
            CoreDsListeners.Remove(id);
        }
        catch (Exception e) { OnDataStorageListenerError?.Invoke(e); }
    }

    private void OnValueChanged(int id, JToken originalValue, JToken newValue,
        Dictionary<string, JToken> additionalArguments)
    {
        if (!DsListeners.TryGetValue(id, out var listener)) return;
        foreach (var action in listener.Values)
        {
            try { action.Invoke(originalValue, newValue, additionalArguments); }
            catch (Exception e) { OnDataStorageListenerError?.Invoke(e); }
        }
    }

    public T? GetFromStorage<T>(string key, Scope scope = Scope.Slot, T? def = default)
        => GetFromStorage(key, (int)scope, def);
    
    public T? GetFromStorage<T>(string key, int scope = -1, T? def = default)
    {
        T? data;
        try { data = GetDataStorageElement(key, scope).GetAsync().Result.ToObject<T>()!; }
        catch (ArgumentException) { data = def; }
        catch (Exception e)
        {
            OnDataStorageListenerError?.Invoke(e);
            data = def;
        }

        return data;
    }

    public void GetFromStorageAsync<T>(string key, Action<T?> callBack, Scope scope = Scope.Slot, T? def = default)
        => GetFromStorageAsync(key, callBack, (int)scope, def);

    public void GetFromStorageAsync<T>(string key, Action<T?> callBack, int scope = -1, T? def = default)
    {
        GetDataStorageElement(key, scope).GetAsync().ContinueWith(obj
                =>
            {
                T? data;
                try { data = obj.Result.ToObject<T>()!; }
                catch (ArgumentException) { data = def; }
                catch (Exception e)
                {
                    OnDataStorageListenerError?.Invoke(e);
                    data = def;
                }
                callBack?.Invoke(data);
            }
        );
    }

    public void SendToStorage<T>(string key, T data, Scope scope = Scope.Slot) => SendToStorage(key, data, (int)scope);

    public void SendToStorage<T>(string key, T data, int scope = -1)
    {
        if (scope is -1) Session!.DataStorage[key] = JsonConvert.SerializeObject(data);
        else Session!.DataStorage[(Scope)scope, key] = JsonConvert.SerializeObject(data);
    }

    private DataStorageElement GetDataStorageElement(string key, int scope = -1)
        => scope is -1 ? Session!.DataStorage[key] : Session!.DataStorage[(Scope)scope, key];
}