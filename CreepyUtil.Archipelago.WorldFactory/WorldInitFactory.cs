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

public class WorldInitFactory(WorldFactory worldFactory,
    string imports = """
                     from .Locations import *
                     from .Rules import *
                     from .Options import *
                     from .Items import *
                     from .Regions import *
                     from .Settings import *
                     from typing import *
                     """)
{
    public string
        InitGeneratorLink
            = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    private WorldFactory WorldFactory = worldFactory;

    private PythonFactory WorldFile = new PythonFactory().AddImport("from worlds.AutoWorld import *")
                                                         .AddImports(imports);

    private PythonClassFactory WorldClass =
        new PythonClassFactory(worldFactory.GameName.Replace(" ", ""))
           .AddComment(worldFactory.GameName).AddParameter("World")
           .AddClass(
                new PythonClassFactory($"{worldFactory.GameName.Replace(" ", "")}Web").AddParameter("WebWorld")
                   .AddVariable(new Variable("tutorials", "[]"))
            )
           .AddVariable(new Variable("game", worldFactory.GameName.Surround('"')))
           .AddVariable(
                new Variable(
                    "options_dataclass",
                    $"{worldFactory.GameName.Replace(" ", "")}Options"
                )
            )
           .AddVariable(new Variable("options", "", $"{worldFactory.GameName.Replace(" ", "")}Options"))
           .AddVariable(
                new Variable(
                    "settings",
                    type: $"ClassVar[{worldFactory.GameName.Replace(" ", "")}Settings]"
                )
            )
           .AddVariable(new Variable("topology_present", "True"));

    public string LocationNameToIdMap = "{value: location_dict.index(value) + 1 for value in location_dict}";
    public string ItemNameToIdMap = "{value: raw_items.index(value) + 1 for value in raw_items}";
    private MethodFactory? InitFunction;
    private GenerateEarlyMethodFactory? GenerateEarly;
    private MethodFactory? CreateRegions;
    private MethodFactory? CreateItem;
    private MethodFactory? CreateItems;
    private SetRulesMethodFactory? SetRules;
    private PreFillMethodFactory? PreFill;
    private FillSlotDataMethodFactory? FillSlotData;
    private GenerateOutputMethodFactory? GenerateOutput;
    private MethodFactory? GetFillerItemName;
    private Dictionary<string, string> ItemNameGroups = [];
    private Dictionary<string, string> LocationNameGroups = [];

    public WorldInitFactory AddImports(string imports)
    {
        WorldFile.AddImports(imports);
        return this;
    }

    public WorldInitFactory UseInitFunction(Action<MethodFactory>? action = null)
    {
        InitFunction ??= new MethodFactory("__init__")
                        .AddParams("self", "multiworld: \"MultiWorld\"", "player: int")
                        .AddCode("super().__init__(multiworld, player)")
                        .AddCode("self.location_count = 0")
                        .AddCode("self.local_location_count = 0");

        action?.Invoke(InitFunction);
        return this;
    }

    public WorldInitFactory UseGenerateEarly(Action<GenerateEarlyMethodFactory>? action = null,
        bool createOptionsIsElse = false)
    {
        var wasNull = GenerateEarly is null;
        GenerateEarly ??= new GenerateEarlyMethodFactory()
                         .AddParam("self")
                         .AddCode("options = self.options");

        if (wasNull && !createOptionsIsElse) GenerateEarly.AddCode("check_options(self)");
        action?.Invoke(GenerateEarly);
        if (wasNull && createOptionsIsElse) GenerateEarly.AddCode("else: check_options(self)");
        return this;
    }

    public WorldInitFactory UseCreateRegions(Action<MethodFactory>? action = null)
    {
        CreateRegions ??= new MethodFactory("create_regions").AddParam("self").AddCode("gen_create_regions(self)");
        action?.Invoke(CreateRegions);
        return this;
    }

    public WorldInitFactory AddUseUniversalTrackerPassthrough(Action<UtCodeBlockFactory>? utBlock = null,
        bool addOptionsFromSlotData = true, bool yamlNeeded = true, bool overrideCreateOptions = true)
    {
        var ut = new UtCodeBlockFactory()
                .AddCode($"if {WorldFactory.GameName.Surround('"')} not in self.multiworld.re_gen_passthrough: return")
                .AddCode($"passthrough = self.multiworld.re_gen_passthrough[{WorldFactory.GameName.Surround('"')}]");

        WorldClass.AddVariable(new Variable("ut_can_gen_without_yaml", yamlNeeded ? "False" : "True"));

        if (addOptionsFromSlotData)
        {
            foreach (var option in WorldFactory.GetOptionsFactory().OptionNames.Keys)
            {
                ut.CreateUtPassthrough(
                    option.FormatStringForOptionsVar(stringify: true),
                    $"options.{option.FormatStringForOptionsVar()}",
                    $"{option.FormatStringForOptionsVar(true)}(passthrough[{option.FormatStringForOptionsVar(stringify: true)}])"
                );
            }
        }

        utBlock?.Invoke(ut);

        UseGenerateEarly(
            factory => factory.AddCode("if hasattr(self.multiworld, \"re_gen_passthrough\"):")
                              .AddCode(ut.GetText(1)), overrideCreateOptions
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

    public WorldInitFactory UseSetRules(Action<SetRulesMethodFactory>? action = null)
    {
        SetRules ??= new SetRulesMethodFactory(WorldFactory)
                    .AddParam("self")
                    .AddCode(new Variable("player", "self.player"))
                    .AddCode(new Variable("options", "self.options"));
        action?.Invoke(SetRules);
        return this;
    }

    public WorldInitFactory UsePreFill(Action<PreFillMethodFactory>? action = null)
    {
        PreFill ??= new PreFillMethodFactory(WorldFactory)
                   .AddParam("self")
                   .AddCode(new Variable("player", "self.player"))
                   .AddCode(new Variable("options", "self.options"));
        action?.Invoke(PreFill);
        return this;
    }

    public WorldInitFactory UseFillSlotData(Dictionary<string, string>? slotData = null,
        Action<FillSlotDataMethodFactory>? action = null, bool stringify = true,
        bool addOptionsToSlotData = true)
    {
        FillSlotData ??= new FillSlotDataMethodFactory().AddParam("self");
        action?.Invoke(FillSlotData);

        Dictionary<string, string> finalData = [];

        if (addOptionsToSlotData)
        {
            foreach (var option in WorldFactory.GetOptionsFactory().OptionNames)
            {
                finalData[option.Key.FormatStringForOptionsVar(stringify: true)] = option.Value.DataType(
                    $"self.options.{option.Key.FormatStringForOptionsVar()}"
                );
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

    public WorldInitFactory UseGetFillerItemNames(params string[] itemNames)
    {
        GetFillerItemName =
            new MethodFactory("get_filler_item_name")
               .AddParam("self").AddCode(
                    $"return [{string.Join(',', itemNames.Select(s => $"\"{s}\""))}][self.random.randint(0,{itemNames.Length - 1})]"
                );
        return this;
    }

    public WorldInitFactory UseGetFillerItemName(string itemName)
    {
        GetFillerItemName = new MethodFactory("get_filler_item_name")
                           .AddParam("self").AddCode($"return \"{itemName}\"");
        return this;
    }

    public WorldInitFactory UseGenerateOutput(Action<GenerateOutputMethodFactory>? action = null)
    {
        GenerateOutput ??= new GenerateOutputMethodFactory()
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

    public WorldInitFactory UseItemGroups(Dictionary<string, ICollection> groups)
        => UseItemGroups(groups.ToDictionary(kv => kv.Key, kv => kv.Value.GetText()));

    public WorldInitFactory UseItemGroups(Dictionary<string, string> groups)
    {
        ItemNameGroups = groups;
        return this;
    }

    public WorldInitFactory UseLocationGroups(Dictionary<string, ICollection> groups)
        => UseLocationGroups(groups.ToDictionary(kv => kv.Key, kv => kv.Value.GetText()));

    public WorldInitFactory UseLocationGroups(Dictionary<string, string> groups)
    {
        LocationNameGroups = groups;
        return this;
    }

    public void GenerateInitFile()
    {
        WorldFile.AddObject(new Comment($"File is Auto-generated, see: [{InitGeneratorLink}]"));

        WorldClass.AddVariable(new Variable("location_name_to_id", LocationNameToIdMap))
                  .AddVariable(new Variable("item_name_to_id", ItemNameToIdMap))
                  .AddVariable(new Variable("enable_local_fill", WorldFactory.LocalFill ? "True" : "False"))
                  .AddVariable(new Variable("local_fill_loc", "[]"));

        if (ItemNameGroups.Count != 0)
        {
            WorldClass.AddVariable(
                new MappedVariable<string, string>(
                    "item_name_groups", ItemNameGroups.ToDictionary(kv => kv.Key.Surround('"'), kv => kv.Value)
                )
            );
        }

        if (LocationNameGroups.Count != 0)
        {
            WorldClass.AddVariable(
                new MappedVariable<string, string>(
                    "location_name_groups",
                    LocationNameGroups.ToDictionary(kv => kv.Key.Surround('"'), kv => kv.Value)
                )
            );
        }

        WorldClass
           .AddMethods(
                InitFunction, GenerateEarly, CreateRegions, CreateItem, CreateItems, SetRules, PreFill, FillSlotData,
                GetFillerItemName, GenerateOutput
            );

        WorldFile.AddObject(WorldClass);
        File.WriteAllText($"{WorldFactory.OutputDirectory}__init__.py", WorldFile.GetText());
    }
}

public class UtCodeBlockFactory : TCodeBlockFactory<UtCodeBlockFactory>
{
    public UtCodeBlockFactory CreateUtPassthrough(string slotDataName, string variable)
        => AddCode(Raw_CreateUtPassthrough(slotDataName, variable));

    public CodeBlockFactory Raw_CreateUtPassthrough(string slotDataName, string variable)
        => Raw_CreateUtPassthrough(slotDataName, variable, $"passthrough[{slotDataName}]");

    public UtCodeBlockFactory CreateUtPassthrough(string slotDataName, string variable, string fromPassthrough)
        => AddCode(Raw_CreateUtPassthrough(slotDataName, variable, fromPassthrough));

    public CodeBlockFactory Raw_CreateUtPassthrough(string slotDataName, string variable, string fromPassthrough)
        => new CodeBlockFactory()
          .AddCode($"if {slotDataName} in passthrough:")
          .AddCode($"\t{variable} = {fromPassthrough}")
          .AddNewLine();
}

public class GenerateEarlyMethodFactory() : MethodFactoryType<GenerateEarlyMethodFactory>("generate_early")
{
    public GenerateEarlyMethodFactory CreatePushPrecollected(string itemName, string condition = "",
        bool stringify = true) => AddCode(Raw_CreatePushPrecollected(itemName, condition, stringify));

    public CodeBlockFactory Raw_CreatePushPrecollected(string itemName, string condition = "", bool stringify = true)
    {
        if (stringify) itemName = itemName.Surround('"');
        return condition is "" ? $"self.multiworld.push_precollected(self.create_item({itemName}))" : $"""
             if {condition}:
                 self.multiworld.push_precollected(self.create_item({itemName}))
             """;
    }
}

public class SetRulesMethodFactory(WorldFactory worldFactory) : MethodFactoryType<SetRulesMethodFactory>("set_rules")
{
    public WorldFactory WorldFactory = worldFactory;

    public SetRulesMethodFactory CreateGoalCondition(string stateLambda)
        => AddCode(Raw_CreateGoalCondition(stateLambda));

    public CodeBlockFactory Raw_CreateGoalCondition(string stateLambda)
        => $"self.multiworld.completion_condition[self.player] = lambda state: {stateLambda}";

    public SetRulesMethodFactory CreateGoalCompiledCondition(string rule)
        => AddCode(Raw_CreateGoalCompiledCondition(rule));

    public CodeBlockFactory Raw_CreateGoalCompiledCondition(string rule)
        => $"self.multiworld.completion_condition[self.player] = lambda state: {WorldFactory.GetRuleFactory().GenerateCompiledRule(rule)}";
}

public class PreFillMethodFactory(WorldFactory worldFactory) : MethodFactoryType<PreFillMethodFactory>("pre_fill")
{
    public WorldFactory WorldFactory = worldFactory;

    public PreFillMethodFactory CreateLocalFill() => AddCode(Raw_CreateLocalFill());

    public CodeBlockFactory Raw_CreateLocalFill()
    {
        return !WorldFactory.LocalFill ? ""
            : $"""
               for _ in range(self.local_location_count):
                   if len(location_map) == 0:
                       raise OptionError(
                           "ㄟ( ▔, ▔ )ㄏ blame other games for touching my speget (aka. other worlds are stealing {WorldFactory.GameName}'s prefill locations)\nbug the developers of the other worlds because they shouldn't be interfering with other worlds like this")
                   
                   location = location_map.pop()
                   try:
                       location.place_locked_item(self.create_item(self.get_filler_item_name()))
                   except:
                       pass
               """;
    }
}

public class FillSlotDataMethodFactory() : MethodFactoryType<FillSlotDataMethodFactory>("fill_slot_data")
{
    public FillSlotDataMethodFactory CreateUniqueId(string variableName = "shuffled", string extra = "")
        => AddCode(Raw_CreateUniqueId(variableName, extra));

    public CodeBlockFactory Raw_CreateUniqueId(string variableName = "shuffled", string extra = "")
        => new CodeBlockFactory()
          .AddCode($"characters = [char for char in f\"{extra}{{self.multiworld.seed}}{{self.player_name}}\"]")
          .AddCode("self.random.shuffle(characters)")
          .AddCode("shuffled = f\"ap_uuid_{''.join(characters).replace(\" \", \"_\")}\"")
          .GetText();

}

public class GenerateOutputMethodFactory() : MethodFactoryType<GenerateOutputMethodFactory>("generate_output")
{
    public GenerateOutputMethodFactory PumlGenCode(string variable = "self.gen_puml")
    {
        return AddCode(
            $$"""
              if {{variable}}: 
                  from Utils import visualize_regions
                  state = self.multiworld.get_all_state(False)
                  state.update_reachable_regions(self.player)
                  visualize_regions(self.get_region("Menu"), f"{self.player_name}_world.puml",
                                    show_entrance_names=True,
                                    regions_to_highlight=state.reachable_regions[self.player])
              """
        );
    }
}