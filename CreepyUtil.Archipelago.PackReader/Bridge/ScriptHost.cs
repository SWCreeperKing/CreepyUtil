using NLua;

namespace CreepyUtil.Archipelago.PackReader.Bridge;

public class ScriptHost(Pack pack)
{
    [LuaHide] public Pack Pack = pack;
    private Dictionary<string, string> LoadedFiles = [];
    private Dictionary<string, Action<string>> CodeWatchers = [];
    private Dictionary<string, Action<double>> FrameWatchers = [];
    private Dictionary<string, Action<string>> LocationSectionWatchers = [];

    public bool LoadScript(string scriptPath)
    {
        try
        {
            if (!LoadedFiles.TryGetValue(scriptPath, out var script))
            {
                LoadedFiles[scriptPath] = script = pack.ReadFile(scriptPath);
            }

            pack.Context.DoString(script);
            return true;
        }
        catch (Exception e)
        {
            Porter.LogInfo($"error loading script: [{scriptPath}]\n{e}");
            return false;
        }
    }

    public string AddWatchForCode(string name, string code, LuaFunction callback)
    {
        CodeWatchers[name] = s =>
        {
            if (s != "*" && s != code) return;
            callback.Call(code);
        };
        return name;
    }

    public bool RemoveWatchForCode(string name) => CodeWatchers.Remove(name);

    public string AddOnFrameHandler(string name, LuaFunction callback)
    {
        FrameWatchers[name] = dt => callback.Call(dt);
        return name;
    }

    public bool RemoveOnFrameHandler(string name) => FrameWatchers.Remove(name);

    public string AddOnLocationSectionChangedHandler(string name, LuaFunction callback)
    {
        LocationSectionWatchers[name] = s => callback.Call(s);
        return name;
    }

    public bool RemoveOnLocationSectionChangedHandler(string name) => LocationSectionWatchers.Remove(name);
    
    #region implement if used, perchance force to sync because NLua

    public void RunScriptAsync(string filePath, object arg, LuaFunction? completePallback,
        LuaFunction? progressCallback)
        => throw new NotImplementedException();

    public void RunStringAsync(string script, object arg, LuaFunction? completePallback, LuaFunction? progressCallback)
        => throw new NotImplementedException();

    #endregion

    #region might not implement (at least **I** might not)

    public string AddMemoryWatch(string name, long address, int length, LuaFunction? callback, int interval)
        => throw new NotImplementedException();

    public bool RemoveMemoryWatch(string name) => throw new NotImplementedException();

    public string AddVariableWatch(string name, string[] variables, LuaFunction? callback, int? interval)
        => throw new NotImplementedException();

    public bool RemoveVariableWatch(string name) => throw new NotImplementedException();

    public void AsyncProgress(object arg) => throw new NotImplementedException();

    #endregion

    [LuaHide]
    public void InvokeCodeWatchers(string code)
    {
        foreach (var (_, watcher) in CodeWatchers)
        {
            watcher(code);
        }
    }

    [LuaHide]
    public void InvokeFrameWatchers(double deltaTime)
    {
        foreach (var (_, watcher) in FrameWatchers)
        {
            watcher(deltaTime);
        }
    }
    
    [LuaHide]
    public void InvokeLocationSectionWatchers(string locationSection)
    {
        foreach (var (_, watcher) in LocationSectionWatchers)
        {
            watcher(locationSection);
        }
    }
}