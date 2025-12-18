using System.Reflection;
using CreepyUtil.Archipelago.PackReader.Bridge;
using CreepyUtil.Archipelago.PackReader.Types;
using NLua;

namespace CreepyUtil.Archipelago.PackReader;

public class Pack(string path, Func<string, string> readFile)
{
    public string BasePath;
    public Func<string, string> ReadFile { get; } = readFile;
    public Manifest Manifest;
    public Lua Context;
    public Tracker Tracker;
    public ImageReference ImageReference;

    public event Action<string>? OnLuaPrintCalled;

    public void CreateLuaContext(string variant)
    {
        Context = new Lua();
        Tracker = new Tracker(this, variant);
        ImageReference = new ImageReference(this);
        
        Context["Tracker"] = Tracker;
        Context["ImageReference"] = ImageReference;
        Context["PopVersion"] = "0.32.1";
        
        Context.RegisterFunction("print", this,
            typeof(Pack).GetMethod("Print", BindingFlags.NonPublic | BindingFlags.Instance));
    }

    private void Print(string text) => OnLuaPrintCalled?.Invoke(text);
}