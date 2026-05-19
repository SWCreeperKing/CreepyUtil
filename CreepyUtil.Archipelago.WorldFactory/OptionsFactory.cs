namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private OptionsFactory? OptionsFactory;

    public OptionsFactory GetOptionsFactory(string link = "No Link Given")
    {
        if (OptionsFactory is not null) return OptionsFactory;
        OptionsFactory = new OptionsFactory(this) { OptionsGeneratorLink = link, };

        return OptionsFactory;
    }
}

public class OptionsFactory(WorldFactory worldFactory)
{
    private WorldFactory WorldFactory = worldFactory;
    public string OptionsGeneratorLink = "No Link Given";

    private List<PythonClassFactory> Options = [];
    private Dictionary<string, HashSet<string>> OptionsGroups = [];

    private PythonClassFactory OptionClass = new PythonClassFactory($"{worldFactory.GameName.Replace(" ", "")}Options")
                                            .AddParameter("PerGameCommonOptions")
                                            .AddDecorator("@dataclass");

    private MethodFactory? CheckOptions = null;
    public Dictionary<string, string> OptionNames { get; private set; } = [];

    public OptionsFactory AddOption(string optionName, string description, IOptionType option, string category = "")
    {
        OptionNames[optionName] = option.DataType();
        Options.Add(
            new PythonClassFactory(optionName.FormatStringForOptionsVar(true))
               .AddComment(description)
               .AddParameter(option.Parameter())
               .AddVariable(new Variable("display_name", optionName.Surround('"')))
               .AddVariables(option.GetData())
        );

        OptionClass.AddVariable(
            new Variable(optionName.FormatStringForOptionsVar(), type: optionName.FormatStringForOptionsVar(true))
        );

        if (category is "") return this;
        if (!OptionsGroups.TryGetValue(category, out var cats)) { OptionsGroups[category] = cats = []; }

        cats.Add(optionName.Replace(" ", ""));
        return this;
    }

    public OptionsFactory AddCheckOptions(Action<MethodFactory>? checkOptions = null)
    {
        CheckOptions ??=
            new MethodFactory("check_options")
               .AddParam("world")
               .AddCode(new Variable("options", "world.options"))
               .AddCode(new Variable("random", "world.random"))
               .AddCode(new Variable("settings", "world.settings"));
        checkOptions?.Invoke(CheckOptions);
        return this;
    }

    public OptionsFactory InjectCodeIntoOptionsClass(Action<PythonClassFactory>? action)
    {
        action?.Invoke(OptionClass);
        return this;
    }

    public void GenerateOptionFile(string fileOutput = "Options.py", string imports = """
        from dataclasses import dataclass
        from Options import *
        from .Locations import *
        """)
    {
        // if (OptionsGroups.Any())
        // {
        //     OptionClass.AddVariable(new Variable("option_groups", $"[{OptionsGroups.Select(kv => $"OptionGroup(\"{kv.Key}\", [{string.Join(", ", kv.Value.Select(s => $"Options"))}])")}]"));
        // }
        MatchFactory optionsMatch = new("option");

        foreach (var option in OptionNames.Keys)
        {
            optionsMatch.AddCase(
                option.FormatStringForOptionsVar().Surround('"'), $"return self.{option.FormatStringForOptionsVar()}"
            );
        }

        if (OptionNames.Count != 0)
        {
            OptionClass.AddMethod(
                new MethodFactory("get_options_map")
                   .AddParams("self", "option")
                   .AddCode(optionsMatch)
            );
        }

        var optionsPy = new PythonFactory()
                       .AddObject(new Comment($"File is Auto-generated, see: [{OptionsGeneratorLink}]"))
                       .AddImports(imports)
                       .AddObjects(Options.ToArray())
                       .AddObject(OptionClass)
                       .AddObject(CheckOptions)
                       .AddObject(
                            new MethodFactory("raise_yaml_error")
                               .AddParams("player_name", "error")
                               .AddCode(
                                    $@"raise OptionError(f'\n\n=== {WorldFactory.GameName} YAML ERROR ===\n{WorldFactory.GameName}: {{player_name}} {{error}}, PLEASE FIX YOUR YAML\n\n')"
                                )
                        );

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", optionsPy.GetText());
    }
}

public class OptionSet(string[] def, string[] collection) : IOptionType
{
    public string DataType() => "str";
    public string Parameter() => "OptionSet";

    public IPythonVariable[] GetData()
        =>
        [
            new Variable("valid_keys", $"frozenset([{string.Join(", ", collection)}])"),
            new Variable("default", $"frozenset([{string.Join(", ", def)}])")
        ];
}

public readonly struct Range(int def, int start, int end) : IOptionType
{
    public string DataType() => "int";

    public string Parameter() => "Range";

    public IPythonVariable[] GetData() =>
    [
        new Variable("range_start", $"{start}"), new Variable("range_end", $"{end}"),
        new Variable("default", $"{def}")
    ];
}

public readonly struct Choice(int def = 0, params string[] choices) : IOptionType
{
    public string DataType() => "int";

    public string Parameter() => "Choice";

    public IPythonVariable[] GetData()
        => choices.Select((s, i) => new Variable($"option_{s}", $"{i}"))
                  .Append(new Variable("default", $"{def}"))
                  .ToArray<IPythonVariable>();
}

public readonly struct DefaultOnToggle : IOptionType
{
    public string DataType() => "bool";

    public string Parameter() => "DefaultOnToggle";
    public IPythonVariable[] GetData() => [];
}

public readonly struct Toggle : IOptionType
{
    public string DataType() => "bool";

    public string Parameter() => "Toggle";
    public IPythonVariable[] GetData() => [];
}

public interface IOptionType
{
    public string DataType();
    public string Parameter();
    public IPythonVariable[] GetData();
}