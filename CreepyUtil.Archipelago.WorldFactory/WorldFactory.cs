using System.Text;

namespace CreepyUtil.Archipelago.WorldFactory;

// this is to aid in creation of apworlds
public partial class WorldFactory
{
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
}