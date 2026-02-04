namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private ItemFactory? ItemFactory;

    public ItemFactory GetItemFactory(string link = "No Link Given")
    {
        if (ItemFactory is not null) return ItemFactory;
        ItemFactory = new ItemFactory(this)
        {
            ItemGeneratorLink = link,
        };

        return ItemFactory;
    }
}

public class ItemFactory(WorldFactory worldFactory)
{
    private static readonly Dictionary<ItemClassification, string> ClassificationMap = new()
    {
        [ItemClassification.Filler] = "filler",
        [ItemClassification.Progression] = "progression",
        [ItemClassification.Useful] = "useful",
        [ItemClassification.Trap] = "trap",
        [ItemClassification.SkipBalancing] = "skip_balancing",
        [ItemClassification.Deprioritized] = "deprioritized",
        [ItemClassification.ProgressionDeprioritizedSkipBalancing] = "progression_deprioritized_skip_balancing",
        [ItemClassification.ProgressionSkipBalancing] = "progression_skip_balancing",
        [ItemClassification.ProgressionDeprioritized] = "progression_deprioritized",
    };

    /// <summary>
    /// What the classifications mean:
    /// https://github.com/ArchipelagoMW/Archipelago/blob/520253e762b179e1e5bd62fe0098d5806120f391/BaseClasses.py#L1535
    /// </summary>
    public enum ItemClassification
    {
        Filler,
        Progression,
        Useful,
        Trap,
        SkipBalancing,
        Deprioritized,
        ProgressionDeprioritizedSkipBalancing,
        ProgressionSkipBalancing,
        ProgressionDeprioritized,
    }

    public string ItemGeneratorLink = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    private WorldFactory WorldFactory = worldFactory;

    private HashSet<string> ItemMap = [];
    private List<IPythonVariable> ItemVariables = [];
    private MethodFactory? CreateItems;

    public ItemFactory AddItem(string item, ItemClassification classification, bool stringify = true)
    {
        ItemMap.Add($"{(stringify ? item.Surround('\"') : item)}: ItemClassification.{ClassificationMap[classification]}");
        return this;
    }

    public ItemFactory AddItems(ItemClassification classification, bool stringify = true, params string[] items) => items.Aggregate(this, (factory, s)
        => factory.AddItem(s, classification, stringify));

    public ItemFactory AddItemCountVariable(string name, Dictionary<string, int> itemsAndCount, ItemClassification classification, bool stringify = true)
    {
        ItemVariables.Add(new MappedVariable<string, int>(name, stringify ? itemsAndCount.ToDictionary(kv => kv.Key.Surround('"'), kv => kv.Value) : itemsAndCount));
        ItemMap.Add($"**{{item: ItemClassification.{ClassificationMap[classification]} for item in {name}}}");
        return this;
    }

    public ItemFactory AddItemListVariable(string name, ItemClassification classification, bool stringify = true, params string[] list)
    {
        ItemVariables.Add(new StringArray(name, list, stringify: stringify));
        ItemMap.Add($"**{{item: ItemClassification.{ClassificationMap[classification]} for item in {name}}}");
        return this;
    }

    public ItemFactory AddCreateItems(Action<MethodFactory> createItems, params string[] parameters)
    {
        CreateItems = new MethodFactory("gen_create_items")
                     .AddParam("world").AddParams(parameters)
                     .AddCode("pool = world.multiworld.itempool")
                     .AddCode("options = world.options");
        createItems(CreateItems);
        return this;
    }

    public void GenerateItemsFile(string fileOutput = "Items.py",
        string imports = """
                         from BaseClasses import ItemClassification
                         from .Locations import *
                         from .Options import *
                         """)
    {
        var itemsPy = new PythonFactory()
                     .AddObject(new Comment($"File is Auto-generated, see: [{ItemGeneratorLink}]"))
                     .AddImports(imports)
                     .AddObjects(ItemVariables.ToArray())
                     .AddObject(new ListedVariableAsMappedVariable<string>("item_table", ItemMap))
                     .AddObject(new Variable("raw_items", "[item for item, classification in item_table.items()]"))
                     .AddObject(CreateItems);

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", itemsPy.GetText());
    }
}