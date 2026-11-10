namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private ItemFactory? ItemFactory;

    public ItemFactory GetItemFactory(string link = "No Link Given")
    {
        if (ItemFactory is not null) return ItemFactory;
        ItemFactory = new ItemFactory(this) { ItemGeneratorLink = link, };

        return ItemFactory;
    }
}

public class ItemFactory(WorldFactory worldFactory)
{
    private static readonly Dictionary<ItemClassification, string> ClassificationMap = new()
    {
        [ItemClassification.Filler] = "filler", [ItemClassification.Progression] = "progression",
        [ItemClassification.Useful] = "useful", [ItemClassification.Trap] = "trap",
        [ItemClassification.SkipBalancing] = "skip_balancing", [ItemClassification.Deprioritized] = "deprioritized",
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
        Filler, Progression, Useful,
        Trap, SkipBalancing, Deprioritized,
        ProgressionDeprioritizedSkipBalancing, ProgressionSkipBalancing, ProgressionDeprioritized,
    }

    public string
        ItemGeneratorLink
            = "No Link Given"; // link to (preferably github) of where you use the location generator, inserts it into the .py

    public WorldFactory WorldFactory = worldFactory;

    private List<string> AllItems = [];
    private HashSet<string> ItemMap = [];
    private List<IPythonVariable> ItemVariables = [];
    private CreateItemsMethodFactory? CreateItems;

    public ItemFactory AddItem(string item, ItemClassification classification, bool stringify = true)
    {
        var realItem = stringify ? item.Surround('\"') : item;
        AllItems.Add(realItem);
        ItemMap.Add($"{realItem}: ItemClassification.{ClassificationMap[classification]}");
        return this;
    }

    public ItemFactory AddItems(ItemClassification classification, bool stringify = true, params string[] items)
        => items.Aggregate(this, (factory, s) => factory.AddItem(s, classification, stringify));

    public ItemFactory AddItemCountVariable(string name, Dictionary<string, int> itemsAndCount,
        ItemClassification classification, bool stringify = true,
        bool addToList = true)
    {
        var map = new MappedVariable<string, int>(
            name, stringify ? itemsAndCount.ToDictionary(kv => kv.Key.Surround('"'), kv => kv.Value) : itemsAndCount
        );

        ItemVariables.Add(map);

        if (!addToList) return this;
        AllItems.AddRange(itemsAndCount.Keys);
        ItemMap.Add($"**{{item: ItemClassification.{ClassificationMap[classification]} for item in {name}}}");
        return this;
    }

    public ItemFactory AddItemListVariable(string name, ItemClassification classification, bool stringify = true,
        bool addToList = true, params string[] list)
    {
        var arr = new StringArray(name, list, stringify: stringify);
        ItemVariables.Add(arr);

        if (!addToList) return this;
        AllItems.AddRange(list);
        ItemMap.Add($"**{{item: ItemClassification.{ClassificationMap[classification]} for item in {name}}}");
        return this;
    }

    public ItemFactory AddCreateItems(Action<CreateItemsMethodFactory> createItems, params string[] parameters)
    {
        CreateItems = new CreateItemsMethodFactory(WorldFactory)
                     .AddParam("world").AddParams(parameters)
                     .AddCode("pool = world.multiworld.itempool")
                     .AddCode("options = world.options");
        createItems(CreateItems);
        return this;
    }

    public ItemFactory AddIndependentVariable(IPythonVariable variable)
    {
        ItemVariables.Add(variable);
        return this;
    }

    public ItemFactory AddToFinalLocationList(string addToFinalList)
    {
        if (addToFinalList is "") return this;
        ItemMap.Add(addToFinalList);
        return this;
    }

    public ItemFactory AddToAllItemsOutput(string item)
    {
        AllItems.Add(item);
        return this;
    }

    public ItemFactory AddToAllItemsOutput(params string[] item)
    {
        AllItems.AddRange(item);
        return this;
    }

    public void GenerateItemsFile(out string[] itemsArray,
        string fileOutput = "Items.py",
        string imports = """
                         import math
                         from BaseClasses import ItemClassification
                         from .Locations import *
                         from .Options import *
                         """)
    {
        itemsArray = [.. AllItems];
        var itemsPy = new PythonFactory()
                     .AddObject(new Comment($"File is Auto-generated, see: [{ItemGeneratorLink}]"))
                     .AddImports(imports)
                     .AddObjects([.. ItemVariables])
                     .AddObject(new ListedVariableAsMappedVariable<string>("item_table", ItemMap))
                     .AddObject(new Variable("raw_items", "[item for item, classification in item_table.items()]"))
                     .AddObject(CreateItems);

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", itemsPy.GetText());
    }
}

public class CreateItemsMethodFactory(WorldFactory worldFactory)
    : MethodFactoryType<CreateItemsMethodFactory>("gen_create_items")
{
    public WorldFactory WorldFactory = worldFactory;

    public CreateItemsMethodFactory CreateItem(string item, bool stringify = true, string amount = "1",
        bool removeFromPool = true)
        => AddCode(Raw_CreateItem(item, stringify, amount, removeFromPool));

    public CodeBlockFactory Raw_CreateItem(string item, bool stringify = true, string amount = "1",
        bool removeFromPool = true)
    {
        var block = new CodeBlockFactory();
        if (removeFromPool) block.AddCode($"world.location_count -= {amount}");

        return block.AddCode($"pool.append(world.create_item({(stringify ? item.Surround('"') : item)}))");
    }


    /// <summary>
    /// makes create items code based on a Dict[str, int]
    /// </summary>
    public CreateItemsMethodFactory CreateItemsFromMapCountGenCode(string collection, bool removeFromPool = true)
        => AddCode(Raw_CreateItemsFromMapCountGenCode(collection, removeFromPool));

    public ForLoopFactory Raw_CreateItemsFromMapCountGenCode(string collection, bool removeFromPool = true)
    {
        var loop = new ForLoopFactory("item, amt", $"{collection}.items()");
        if (removeFromPool) loop.AddCode("world.location_count -= amt");

        return loop.AddCode(new ForLoopFactory("_", "range(amt)").AddCode("pool.append(world.create_item(item))"));
    }


    public CreateItemsMethodFactory CreateItemsFromCountGenCode(string amount, string item, bool stringify = true,
        bool removeFromPool = true)
        => AddCode(Raw_CreateItemsFromCountGenCode(amount, item, stringify, removeFromPool));

    public ForLoopFactory Raw_CreateItemsFromCountGenCode(string amount, string item, bool stringify = true,
        bool removeFromPool = true)
        => new ForLoopFactory("_", $"range({amount})")
           .AddCode(Raw_CreateItem(item, stringify, removeFromPool: removeFromPool));


    public CreateItemsMethodFactory FillRemainingWith(string collection) => AddCode(Raw_FillRemainingWith(collection));

    public ForLoopFactory Raw_FillRemainingWith(string collection)
        => new ForLoopFactory("_", "range(world.location_count)").AddCode(
            $"pool.append(world.create_item(world.random.choice({collection})))"
        );


    public CreateItemsMethodFactory CreateItemForLoop(string collection, string forVariable = "item",
        string createItemVariable = "item", string exclusionCondition = "", bool stringify = false, string amount = "1",
        bool removeFromPool = true) => AddCode(
        Raw_CreateItemForLoop(
            collection, forVariable, createItemVariable, exclusionCondition, stringify, amount, removeFromPool
        )
    );

    public ForLoopFactory Raw_CreateItemForLoop(string collection, string forVariable = "item",
        string createItemVariable = "item", string exclusionCondition = "", bool stringify = false, string amount = "1",
        bool removeFromPool = true)
    {
        var forLoop = new ForLoopFactory(forVariable, collection);

        if (exclusionCondition is not "") forLoop.AddCode(new IfFactory(exclusionCondition).AddCode("continue"));

        return forLoop.AddCode(Raw_CreateItem(createItemVariable, stringify, amount, removeFromPool));
    }


    public CreateItemsMethodFactory CreateItemsFromClassificationList(string collection = "item_table.items()",
        bool removeFromPool = true, string exclusionCondition = "")
        => AddCode(Raw_CreateItemsFromClassificationList(collection, removeFromPool, exclusionCondition));

    public ForLoopFactory Raw_CreateItemsFromClassificationList(string collection = "item_table.items()",
        bool removeFromPool = true, string exclusionCondition = "") => Raw_CreateItemForLoop(
        collection, "item, classification", exclusionCondition: exclusionCondition, removeFromPool: removeFromPool
    );


    public CreateItemsMethodFactory CreateItemsFromList(string collection, bool removeFromPool = true,
        string exclusionCondition = "")
        => AddCode(Raw_CreateItemsFromList(collection, removeFromPool, exclusionCondition));

    public ForLoopFactory Raw_CreateItemsFromList(string collection, bool removeFromPool = true,
        string exclusionCondition = "") => Raw_CreateItemForLoop(
        collection, exclusionCondition: exclusionCondition, removeFromPool: removeFromPool
    );


    public CreateItemsMethodFactory FillRemainingWithItem(string item, bool stringify = true)
        => AddCode(Raw_FillRemainingWithItem(item, stringify));

    public ForLoopFactory Raw_FillRemainingWithItem(string item, bool stringify = true)
        => new ForLoopFactory("_", "range(world.location_count)").AddCode(
            $"pool.append(world.create_item({(stringify ? item.Surround('"') : item)}))"
        );


    public CreateItemsMethodFactory BalanceLocalFill()
        => !WorldFactory.LocalFill ? this : AddCode(Raw_BalanceLocalFill());

    public IfFactory Raw_BalanceLocalFill() =>
        new IfFactory("world.enable_local_fill")
           .AddCode("fill = min(math.ceil(world.location_count * (options.local_fill.value/100)), len(world.local_fill_loc))")
           .AddCode("world.location_count -= fill")
           .AddCode("world.local_location_count += fill");
}