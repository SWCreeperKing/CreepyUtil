namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private OptionsFactory? OptionsFactory;

    public OptionsFactory GetOptionsFactory(string link = "No Link Given")
    {
        if (OptionsFactory is not null) return OptionsFactory;
        OptionsFactory = new OptionsFactory(this)
        {
            OptionsGeneratorLink = link,
        };

        return OptionsFactory;
    }
}

public class OptionsFactory(WorldFactory worldFactory)
{
    private WorldFactory WorldFactory = worldFactory;
    public string OptionsGeneratorLink = "No Link Given";

    private List<PythonClassFactory> Options = [];

    private PythonClassFactory OptionClass = new PythonClassFactory($"{worldFactory.GameName.Replace(" ", "")}Options")
                                            .AddParameter("PerGameCommonOptions")
                                            .AddDecorator("@dataclass");

    private MethodFactory? CheckOptions = null;

    public OptionsFactory AddOption(string optionName, string description, IOptionType option)
    {
        Options.Add(new PythonClassFactory(optionName.Replace(" ", ""))
                   .AddComment(description)
                   .AddParameter(option.Parameter())
                   .AddVariable(new Variable("display_name", optionName.Surround('"')))
                   .AddVariables(option.GetData()));

        OptionClass.AddVariable(new Variable(optionName.Replace(" ", "_").ToLower(), type: optionName.Replace(" ", "")));
        return this;
    }

    public OptionsFactory AddCheckOptions(MethodFactory checkOptions)
    {
        CheckOptions = checkOptions;
        return this;
    }

    public void GenerateOptionFile(string fileOutput = "Options.py")
    {
        var optionsPy = new PythonFactory()
                       .AddObject(new Comment($"File is Auto-generated, see: [{OptionsGeneratorLink}]"))
                       .AddImports("from dataclasses import dataclass", "from Options import Range, Toggle, DefaultOnToggle, PerGameCommonOptions, OptionSet, OptionError, Choice, Accessibility")
                       .AddObjects(Options.ToArray())
                       .AddObject(OptionClass)
                       .AddObject(CheckOptions)
                       .AddObject(new MethodFactory("raise_yaml_error")
                                 .AddParams("player_name", "error")
                                 .AddCode($@"raise OptionError(f'\n\n=== {WorldFactory.GameName} YAML ERROR ===\n{WorldFactory.GameName}: {{player_name}} {{error}}, PLEASE FIX YOUR YAML\n\n')"));

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", optionsPy.GetText());
    }
}

public readonly struct Range(int def, int start, int end) : IOptionType
{
    public string Parameter() => "Range";

    public IPythonVariable[] GetData() => [new Variable("range_start", $"{start}"), new Variable("range_end", $"{end}"), new Variable("default", $"{def}")];
}

public readonly struct Choice(params string[] choices) : IOptionType
{
    public string Parameter() => "Choice";
    public IPythonVariable[] GetData() => choices.Select((s, i) => new Variable($"option_{s}", $"{i}")).ToArray();
}

public readonly struct DefaultOnToggle : IOptionType
{
    public string Parameter() => "DefaultOnToggle";
    public IPythonVariable[] GetData() => [];
}

public readonly struct Toggle : IOptionType
{
    public string Parameter() => "Toggle";
    public IPythonVariable[] GetData() => [];
}

public interface IOptionType
{
    public string Parameter();
    public IPythonVariable[] GetData();
}