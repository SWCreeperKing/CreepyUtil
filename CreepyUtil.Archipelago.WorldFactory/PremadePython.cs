namespace CreepyUtil.Archipelago.WorldFactory;

public static class PremadePython
{
    public static string StateHas(
        string item, string count = "1", bool stringify = true, bool returnValue = true, string player = "player",
        string state = "state"
    )
        => $"{(returnValue ? "return " : "")}{state}.has({(stringify ? item.Surround("\"") : item)}, {player}, {count})";

    public static string StateHasAll(
        string collection, bool returnValue = true, string player = "player", string state = "state"
    )
        => $"{(returnValue ? "return " : "")}{state}.has_all({collection}, {player})";

    public static string StateHasAny(
        string collection, bool returnValue = true, string player = "player", string state = "state"
    )
        => $"{(returnValue ? "return " : "")}{state}.has_any({collection}, {player})";

    public static string PumlGenCode(string variable = "self.gen_puml")
    {
        return $$"""
                 if {{variable}}: 
                     from Utils import visualize_regions
                     state = self.multiworld.get_all_state(False)
                     state.update_reachable_regions(self.player)
                     visualize_regions(self.get_region("Menu"), f"{self.player_name}_world.puml",
                                       show_entrance_names=True,
                                       regions_to_highlight=state.reachable_regions[self.player])
                 """;
    }

    /// <summary>
    /// makes create items code based on a Dict[str, int]
    /// </summary>
    public static string CreateItemsFromMapCountGenCode(string collection, bool removeFromPool = true)
    {
        var loop = new ForLoopFactory("item, amt", $"{collection}.items()");
        if (removeFromPool) loop.AddCode("world.location_count -= amt");

        return loop.AddCode(
                        new ForLoopFactory("_", "range(amt)")
                           .AddCode("pool.append(world.create_item(item))")
                    )
                   .ToString();
    }

    public static string CreateItemsFromCountGenCode(
        string amount, string item, bool stringify = true, bool removeFromPool = true
    )
    {
        return new ForLoopFactory("_", $"range({amount})")
              .AddCode(CreateItem(item, stringify, removeFromPool: removeFromPool))
              .ToString();
    }

    public static string CreateItemsFillRemainingWith(string collection)
    {
        return new ForLoopFactory("_", "range(world.location_count)")
              .AddCode($"pool.append(world.create_item(world.random.choice({collection})))").ToString();
    }

    public static string CreateItemsFromClassificationList(
        string collection = "item_table.items()", bool removeFromPool = true, string exclusionCondition = ""
    )
    {
        return CreateItemForLoop(
            collection, "item, classification", exclusionCondition: exclusionCondition, removeFromPool: removeFromPool
        );
    }

    public static string CreateItemsFromList(string collection, bool removeFromPool = true, string exclusionCondition = "")
    {
        return CreateItemForLoop(collection, exclusionCondition: exclusionCondition, removeFromPool: removeFromPool);
    }

    public static string CreateItemForLoop(
        string collection, string forVariable = "item", string createItemVariable = "item",
        string exclusionCondition = "", bool stringify = false, string amount = "1", bool removeFromPool = true
    )
    {
        var forLoop = new ForLoopFactory(forVariable, collection);

        if (exclusionCondition is not "") forLoop.AddCode(new IfFactory(exclusionCondition).AddCode("continue"));

        return forLoop.AddCode(CreateItem(createItemVariable, stringify, amount, removeFromPool))
                      .ToString();
    }

    public static string CreateItem(string item, bool stringify = true, string amount = "1", bool removeFromPool = true)
    {
        var block = new CodeBlockFactory();
        if (removeFromPool) block.AddCode($"world.location_count -= {amount}");

        return block.AddCode($"pool.append(world.create_item({(stringify ? item.Surround('"') : item)}))").ToString();
    }

    public static string CreateItemsFillRemainingWithItem(string item, bool stringify = true)
    {
        return new ForLoopFactory("_", "range(world.location_count)")
              .AddCode($"pool.append(world.create_item({(stringify ? item.Surround('"') : item)}))").ToString();
    }

    public static string CreateUniqueId(string variableName = "shuffled", string extra = "")
    {
        return new CodeBlockFactory().AddCode(
                                          $"characters = [char for char in f\"{extra}{{self.multiworld.seed}}{{self.player_name}}\"]"
                                      )
                                     .AddCode("self.random.shuffle(characters)")
                                     .AddCode("shuffled = f\"ap_uuid_{''.join(characters).replace(\" \", \"_\")}\"")
                                     .GetText();
    }

    public static string CreatePushPrecollected(string itemName, string condition = "", bool stringify = true)
    {
        if (stringify) itemName = itemName.Surround('"');
        if (condition is "") return $"self.multiworld.push_precollected(self.create_item({itemName}))";
        return $"""
                if {condition}:
                    self.multiworld.push_precollected(self.create_item({itemName}))
                """;
    }

    public static string CreateUtPassthrough(string slotDataName, string variable)
    {
        return CreateUtPassthrough(slotDataName, variable, $"passthrough[{slotDataName}]");
    }

    public static string CreateUtPassthrough(string slotDataName, string variable, string fromPassthrough)
    {
        return new CodeBlockFactory()
              .AddCode($"if {slotDataName} in passthrough:")
              .AddCode($"\t{variable} = {fromPassthrough}")
              .AddNewLine()
              .GetText();
    }

    public static string CreateGoalCondition(string stateLambda)
    {
        return $"self.multiworld.completion_condition[self.player] = lambda state: {stateLambda}";
    }

    public static string CreateGoalCondition(string rule, RuleFactory factory)
    {
        return
            $"self.multiworld.completion_condition[self.player] = lambda state: {factory.GenerateCompiledRule(rule)}";
    }

    public static string CreateMinimalCatch(string gameName)
    {
        return $"""
                if options.accessibility == Accessibility.option_minimal:
                    print("{gameName} doesn't support accessibility minimal, defaulting accessibility to full")
                    options.accessibility = Accessibility(Accessibility.option_full)
                """;
    }
}