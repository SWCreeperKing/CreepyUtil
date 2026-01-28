namespace CreepyUtil.Archipelago.WorldFactory;

public static class PremadePython
{
    public static string StateHasS(string item, string count = "1", string player = "player", string state = "state")
    {
        return $"{state}.has({item.Surround("\"")}, {player}, {count})";
    }

    public static string StateHas(string item, string count = "1", string player = "player", string state = "state")
    {
        return $"{state}.has({item}, {player}, {count})";
    }

    public static string StateHasSR(string item, string count = "1", string player = "player", string state = "state")
    {
        return $"return {state}.has({item.Surround("\"")}, {player}, {count})";
    }

    public static string StateHasR(string item, string count = "1", string player = "player", string state = "state")
    {
        return $"return {state}.has({item}, {player}, {count})";
    }

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
    public static string CreateItemsFromMapCountGenCode(string collection)
    {
        return new ForLoopFactory("item, amt", $"{collection}.items()")
              .AddCode("world.location_count -= amt")
              .AddCode(new ForLoopFactory("_", "range(amt)")
                  .AddCode("pool.append(world.create_item(item))")).ToString();
    }

    public static string CreateItemsFromCountGenCode(string amount, string item, bool stringify = true)
    {
        return new ForLoopFactory("_", $"range({amount})")
              .AddCode("world.location_count -= 1")
              .AddCode($"pool.append(world.create_item({(stringify ? item.Surround('"') : item)}))").ToString();
    }

    public static string CreateItemsFillRemainingWith(string collection)
    {
        return new ForLoopFactory("_", "range(world.location_count)")
              .AddCode($"pool.append(world.create_item(world.random.choice({collection})))").ToString();
    }

    public static string CreateUniqueId(string variableName = "shuffled", string extra = "")
    {
        return new CodeBlockFactory().AddCode($"characters = [char for char in f\"{extra}{{self.multiworld.seed}}{{self.player_name}}\"]")
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
}