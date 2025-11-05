using CreepyUtil.Archipelago.PackReader.Bridge;
using CreepyUtil.Archipelago.PackReader.Types;
using NLua;

namespace CreepyUtil.Archipelago.PackReader;

public class Pack(Func<string, string> readFile)
{
    public Func<string, string> ReadFile { get; } = readFile;
    public Manifest Manifest;
    public Lua Context;
    public Tracker Tracker;

    public void CreateLuaContext(string variant)
    {
        Context = new();

        Tracker = new Tracker(this, variant);
        Context["Tracker"] = Tracker;
    }
}