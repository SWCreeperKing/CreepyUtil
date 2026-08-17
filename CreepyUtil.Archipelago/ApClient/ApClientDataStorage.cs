using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    private Dictionary<string, DataStorageHelper.DataStorageUpdatedHandler> CoreDsListeners = [];
    private Dictionary<string, HashSet<DataStorageHelper.DataStorageUpdatedHandler>> DsListeners = [];

    public void AddDataStorageListener(string key, DataStorageHelper.DataStorageUpdatedHandler action)
    {
        if (DsListeners.ContainsKey(key)) DsListeners[key].Add(action);
        else DsListeners[key] = [action];

        if (CoreDsListeners.ContainsKey(key)) return;
        DataStorageHelper.DataStorageUpdatedHandler listener = (o, n, d) => OnOnValueChanged(key, o, n, d);
        Session!.DataStorage[key].OnValueChanged += listener;
        CoreDsListeners[key] = listener;
    }

    public void RemoveDataStorageListeners(string key)
    {
        DsListeners.Remove(key);
        if (!CoreDsListeners.TryGetValue(key, out var listener)) return;
        Session!.DataStorage[key].OnValueChanged -= listener;
        CoreDsListeners.Remove(key);
    }

    private void OnOnValueChanged(string key, JToken originalValue, JToken newValue,
        Dictionary<string, JToken> additionalArguments)
    {
        foreach (var action in DsListeners[key]) action?.Invoke(originalValue, newValue, additionalArguments);
    }

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