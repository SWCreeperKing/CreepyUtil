namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory(string gameName)
{
    public string GameName { get; } = gameName;

    private PythonFactory Init = new PythonFactory();

    private WorldInitFactory? InitFactory;

    public WorldInitFactory GetInitFactory(string link = "No Link Given")
    {
        if (InitFactory is not null) return InitFactory;
        InitFactory = new WorldInitFactory(this) { InitGeneratorLink = link, };

        return InitFactory;
    }

    public void GenerateArchipelagoJson(string minApVersion, string worldVersion, params string[] authors)
    {
        var json = $$"""
                     {
                       "game": "{{GameName}}",
                       "minimum_ap_version": "{{minApVersion}}",
                       "world_version": "{{worldVersion}}",
                       "authors": [{{string.Join(", ", authors.Select(s => s.Surround('"')))}}]
                     }
                     """;
        
        File.WriteAllText($"{OutputDirectory}archipelago.json", json);
    }
}

public class WorldInitFactory
(
    WorldFactory worldFactory, string imports = """
                                                from .Locations import *
                                                from .Rules import *
                                                from .Options import *
                                                from .Items import *
                                                from .Regions import *
                                                """
)
{
    public string InitGeneratorLink
        = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    private WorldFactory WorldFactory = worldFactory;

    private PythonFactory WorldFile = new PythonFactory().AddImport("from worlds.AutoWorld import World")
                                                         .AddImports(imports);

    private PythonClassFactory WorldClass
        = new PythonClassFactory(worldFactory.GameName.Replace(" ", ""))
         .AddComment(worldFactory.GameName).AddParameter("World")
         .AddVariable(new Variable("game", worldFactory.GameName.Surround('"')))
         .AddVariable(new Variable("options_dataclass", $"{worldFactory.GameName.Replace(" ", "")}Options"))
         .AddVariable(new Variable("options", "", $"{worldFactory.GameName.Replace(" ", "")}Options"))
         .AddVariable(
              new Variable("location_name_to_id", "{value: location_dict.index(value) + 1 for value in location_dict}")
          )
         .AddVariable(new Variable("item_name_to_id", "{value: raw_items.index(value) + 1 for value in raw_items}"))
         .AddVariable(new Variable("topology_present", "True"));

    private MethodFactory? InitFunction;
    private MethodFactory? GenerateEarly;
    private MethodFactory? CreateRegions;
    private MethodFactory? CreateItem;
    private MethodFactory? CreateItems;
    private MethodFactory? SetRules;
    private MethodFactory? FillSlotData;
    private MethodFactory? GenerateOutput;

    public WorldInitFactory AddImports(string imports)
    {
        WorldFile.AddImports(imports);
        return this;
    }

    public WorldInitFactory AddItemNameGroups(Dictionary<string, string> groups, bool stringify = true)
    {
        WorldClass.AddVariable(
            new MappedVariable<string, string>(
                "item_name_groups", stringify ? groups.ToDictionary(kv => kv.Key.Surround('"'), kv => kv.Value) : groups
            )
        );
        return this;
    }

    public WorldInitFactory UseInitFunction(Action<MethodFactory>? action = null)
    {
        InitFunction ??= new MethodFactory("__init__")
                        .AddParams("self", "multiworld: \"MultiWorld\"", "player: int")
                        .AddCode("super().__init__(multiworld, player)")
                        .AddCode("self.location_count = 0");

        action?.Invoke(InitFunction);
        return this;
    }

    public WorldInitFactory UseGenerateEarly(Action<MethodFactory>? action = null)
    {
        GenerateEarly ??= new MethodFactory("generate_early")
                         .AddParam("self")
                         .AddCode("check_options(self)");

        action?.Invoke(GenerateEarly);
        return this;
    }

    public WorldInitFactory UseCreateRegions(Action<MethodFactory>? action = null)
    {
        CreateRegions ??= new MethodFactory("create_regions").AddParam("self").AddCode("gen_create_regions(self)");
        action?.Invoke(CreateRegions);
        return this;
    }

    public WorldInitFactory AddUseUniversalTrackerPassthrough(
        Action<CodeBlockFactory>? utBlock = null, bool addOptionsFromSlotData = true
    )
    {
        var ut = new CodeBlockFactory()
                .AddCode($"if {WorldFactory.GameName.Surround('"')} not in self.multiworld.re_gen_passthrough: return")
                .AddCode($"passthrough = self.multiworld.re_gen_passthrough[{WorldFactory.GameName.Surround('"')}]");

        if (addOptionsFromSlotData)
        {
            foreach (var option in WorldFactory.GetOptionsFactory().OptionNames.Keys)
            {
                ut.AddCode($"if {option.ToLower().Replace(" ", "_").Surround('"')} in passthrough:")
                  .AddCode(
                       $"\tself.options.{option.ToLower().Replace(" ", "_")} = {option.Replace(" ", "")}(passthrough[{option.ToLower().Replace(" ", "_").Surround('"')}])"
                   )
                  .AddNewLine();
            }
        }

        utBlock?.Invoke(ut);

        UseGenerateEarly(factory => factory.AddCode("if hasattr(self.multiworld, \"re_gen_passthrough\"):")
                                           .AddCode(ut.GetText(1))
        );

        return this;
    }

    public WorldInitFactory AddCreateItems()
    {
        CreateItem ??= new MethodFactory("create_item")
                      .AddParams("self", "name: str")
                      .AddCode("return Item(name, item_table[name], self.item_name_to_id[name], self.player)");

        CreateItems ??= new MethodFactory("create_items")
                       .AddParam("self")
                       .AddCode("gen_create_items(self)");

        return this;
    }

    public WorldInitFactory UseSetRules(Action<MethodFactory>? action = null)
    {
        SetRules ??= new MethodFactory("set_rules").AddParam("self");
        action?.Invoke(SetRules);
        return this;
    }

    public WorldInitFactory UseFillSlotData(
        Dictionary<string, string>? slotData = null, Action<MethodFactory>? action = null, bool stringify = true,
        bool addOptionsToSlotData = true
    )
    {
        FillSlotData ??= new MethodFactory("fill_slot_data").AddParam("self");
        action?.Invoke(FillSlotData);

        Dictionary<string, string> finalData = [];

        if (addOptionsToSlotData)
        {
            foreach (var option in WorldFactory.GetOptionsFactory().OptionNames)
            {
                finalData[option.Key.Replace(" ", "_").ToLower().Surround('"')]
                    = $"{option.Value}(self.options.{option.Key.Replace(" ", "_").ToLower()})";
            }
        }

        if (slotData is not null)
        {
            foreach (var kv in slotData) { finalData[stringify ? kv.Key.Surround('"') : kv.Key] = kv.Value; }
        }

        FillSlotData.AddCode(new MappedVariable<string, string>("slot_data", finalData))
                    .AddCode("return slot_data");

        return this;
    }

    public WorldInitFactory UseGenerateOutput(Action<MethodFactory>? action = null)
    {
        GenerateOutput ??= new MethodFactory("generate_output")
           .AddParams("self", "output_directory: str");
        action?.Invoke(GenerateOutput);
        return this;
    }

    public WorldInitFactory InjectCodeIntoWorld(Action<PythonClassFactory> action)
    {
        action(WorldClass);
        return this;
    }

    public WorldInitFactory InjectCodeIntoFile(Action<PythonFactory> action)
    {
        action(WorldFile);
        return this;
    }

    public void GenerateInitFile()
    {
        WorldFile.AddObject(new Comment($"File is Auto-generated, see: [{InitGeneratorLink}]"));

        WorldClass
           .AddMethods(
                InitFunction, GenerateEarly, CreateRegions, CreateItem, CreateItems, SetRules, FillSlotData,
                GenerateOutput
            );

        WorldFile.AddObject(WorldClass);
        File.WriteAllText($"{WorldFactory.OutputDirectory}__init__.py", WorldFile.GetText());
    }
}