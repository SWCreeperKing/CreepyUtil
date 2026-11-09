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

    private CreateOptionsMethodFactory? CheckOptions = null;
    public Dictionary<string, IOptionType> OptionNames { get; private set; } = [];

    public OptionsFactory AddOption(string optionName, string description, IOptionType option, string category = "")
    {
        OptionNames[optionName] = option;
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

    public OptionsFactory AddCheckOptions(Action<CreateOptionsMethodFactory>? checkOptions = null)
    {
        CheckOptions ??=
            new CreateOptionsMethodFactory(WorldFactory)
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
        if (WorldFactory.LocalFill)
            AddOption(
                "Local Fill", "% of filler to be made local",
                new Range(WorldFactory.LocalDefault, WorldFactory.LocalFillMin, 100)
            );
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
                       .AddObjects([.. Options])
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

public class CreateOptionsMethodFactory(WorldFactory worldFactory)
    : MethodFactoryType<CreateOptionsMethodFactory>("check_options")
{
    public WorldFactory WorldFactory = worldFactory;

    public CreateOptionsMethodFactory CreateMinimalCatch() => AddCode(Raw_CreateMinimalCatch());

    public CodeBlockFactory Raw_CreateMinimalCatch() => new CodeBlockFactory().AddCode(
        $"""
         if options.accessibility == Accessibility.option_minimal:
             print("{worldFactory.GameName} doesn't support accessibility minimal, defaulting accessibility to full")
             options.accessibility = Accessibility(Accessibility.option_full)
         """
    );
}

public class OptionSet(string[] def, string[] collection) : IOptionType
{
    public string DataType(string val) => $"str(sorted({val}))";
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
    public string DataType(string val) => $"int({val})";

    public string Parameter() => "Range";

    public IPythonVariable[] GetData() =>
    [
        new Variable("range_start", $"{start}"), new Variable("range_end", $"{end}"),
        new Variable("default", $"{def}")
    ];
}

public readonly struct Choice(int def = 0, params string[] choices) : IOptionType
{
    public string DataType(string val) => $"int({val})";

    public string Parameter() => "Choice";

    public IPythonVariable[] GetData()
        => [.. choices.Select((s, i) => new Variable($"option_{s}", $"{i}")), new Variable("default", $"{def}")];
}

public readonly struct DefaultOnToggle : IOptionType
{
    public string DataType(string val) => $"bool({val})";

    public string Parameter() => "DefaultOnToggle";
    public IPythonVariable[] GetData() => [];
}

public readonly struct Toggle : IOptionType
{
    public string DataType(string val) => $"bool({val})";

    public string Parameter() => "Toggle";
    public IPythonVariable[] GetData() => [];
}

public interface IOptionType
{
    public string DataType(string val);
    public string Parameter();
    public IPythonVariable[] GetData();
}