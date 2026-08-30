using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    public event Action<Exception>? OnDataStorageListenerError;
    private Dictionary<int, DataStorageHelper.DataStorageUpdatedHandler> CoreDsListeners = [];
    private Dictionary<int, Dictionary<string, DataStorageHelper.DataStorageUpdatedHandler>> DsListeners = [];

    public bool ContainsDataStorageListener(string key, string functionId, Scope scope = Scope.Global)
    {
        var id = (key, scope).GetHashCode();
        if (!CoreDsListeners.ContainsKey(id)) return false;
        return DsListeners.TryGetValue(id, out var s) && s.ContainsKey(functionId);
    }

    public void AddDataStorageListener(string key, string functionId,
        DataStorageHelper.DataStorageUpdatedHandler action, Scope scope = Scope.Global)
    {
        try
        {
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
            Session!.DataStorage[scope, key].OnValueChanged += listener;
            CoreDsListeners[id] = listener;
        }
        catch (Exception e) { OnDataStorageListenerError?.Invoke(e); }
    }

    public void RemoveDataStorageListeners(string key, string functionId, Scope scope = Scope.Global)
    {
        try
        {
            var id = (key, scope).GetHashCode();
            if (!DsListeners.TryGetValue(id, out var dsListeners)) return;
            if (!dsListeners.Remove(functionId)) return;
            if (DsListeners[id].Count != 0) return;
            if (!CoreDsListeners.TryGetValue(id, out var listener)) return;

            Session!.DataStorage[key].OnValueChanged -= listener;
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
    {
        T? data;
        // try { data = JsonConvert.DeserializeObject<T>(Session!.DataStorage[scope, key].To<string>())!; }
        try { data = Session!.DataStorage[scope, key].GetAsync().Result.ToObject<T>()!; }
        catch (ArgumentException) { data = def; }
        catch (Exception e)
        {
            OnDataStorageListenerError?.Invoke(e);
            data = def;
        }

        return data;
    }

    public void GetFromStorageAsync<T>(string key, Action<T?> callBack, Scope scope = Scope.Slot, T? def = default)
    {
        Session!.DataStorage[scope, key].GetAsync().ContinueWith(obj =>
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

    public void SendToStorage<T>(string key, T data, Scope scope = Scope.Slot)
        => Session!.DataStorage[scope, key] = JsonConvert.SerializeObject(data);
}