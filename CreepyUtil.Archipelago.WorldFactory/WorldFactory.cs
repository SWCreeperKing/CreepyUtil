namespace CreepyUtil.Archipelago.WorldFactory;

// this is to aid in creation of apworlds
public partial class WorldFactory
{
    public bool LocalFill = false;
    public int LocalFillMin;
    public int LocalDefault;
    public string OutputDirectory = "";
    public Action<Exception, string>? OnCompilerError { get; private set; }

    public WorldFactory SetOutputDirectory(string dir)
    {
        dir = dir.Replace(@"\\", "/");
        OutputDirectory = dir.EndsWith("/") ? dir : $"{dir}/";
        return this;
    }

    public WorldFactory SetOnCompilerError(Action<Exception, string>? onError)
    {
        OnCompilerError = onError;
        return this;
    }

    public WorldFactory EnableLocalFill(int min, int def)
    {
        LocalFill = true;
        LocalFillMin = min;
        LocalDefault = def;
        return this;
    }
}