namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory(string gameName)
{
    public string GameName { get; } = gameName;

    private PythonFactory Init = new PythonFactory();
}