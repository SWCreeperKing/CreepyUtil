namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private HostSettingsFactory? HostSettingsFactory;

    public HostSettingsFactory GetHostSettingsFactory(string link = "No Link Given")
    {
        if (HostSettingsFactory is not null) return HostSettingsFactory;
        HostSettingsFactory = new HostSettingsFactory(this) { HostSettingsGeneratorLink = link, };

        return HostSettingsFactory;
    }
}

public class HostSettingsFactory(WorldFactory worldFactory)
{
    private WorldFactory WorldFactory = worldFactory;
    public string HostSettingsGeneratorLink = "No Link Given";

    private List<IHostSetting> Settings = [];

    public HostSettingsFactory AddSetting(IHostSetting setting)
    {
        Settings.Add(setting);
        return this;
    }

    public HostSettingsFactory AddSettings(params IHostSetting[] settings)
    {
        Settings.AddRange(settings);
        return this;
    }

    public void GenerateHostSettingsFile(
        string fileOutput = "Settings.py", string imports = "from settings import Group, Bool"
    )
    {
        var settingsClass
            = new PythonClassFactory($"{worldFactory.GameName.Replace(" ", "")}Settings").AddParameter("Group");

        foreach (var setting in Settings)
        {
            settingsClass.AddClass(setting.GetClass());
            settingsClass.AddVariable(setting.GetVariable());
        }
        
        var settingsPy = new PythonFactory()
                        .AddObject(new Comment($"File is Auto-generated, see: [{HostSettingsGeneratorLink}]"))
                        .AddImports(imports).AddObject(settingsClass);

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", settingsPy.GetText());
    }
}

public readonly struct Bool(string settingName, string comment, bool value) : IHostSetting
{
    public PythonClassFactory GetClass() => new PythonClassFactory(settingName.Replace(" ", ""))
                                           .AddParameter("Bool")
                                           .AddComment(comment);

    public IPythonVariable GetVariable() => new Variable(
        settingName.ToLower().Replace(" ", ""), value ? "True" : "False"
    );
}

public interface IHostSetting
{
    public PythonClassFactory GetClass();

    public IPythonVariable GetVariable();
}