namespace CreepyUtil.Archipelago.WorldFactory;

public partial class WorldFactory
{
    private RegionFactory? RegionFactory;

    public RegionFactory GetRegionFactory(string link = "No Link Given")
    {
        if (RegionFactory is not null) return RegionFactory;
        RegionFactory = new RegionFactory(this) { RegionGeneratorLink = link, };

        return RegionFactory;
    }
}

public class RegionFactory(WorldFactory worldFactory)
{
    private static readonly Dictionary<LocationProgressType, string> PriorityMap = new()
    {
        [LocationProgressType.Default] = "DEFAULT", [LocationProgressType.Priority] = "PRIORITY",
        [LocationProgressType.Excluded] = "EXCLUDED",
    };

    public enum LocationProgressType
    {
        Default, Priority, Excluded,
    }

    private WorldFactory WorldFactory = worldFactory;
    public string RegionGeneratorLink = "No Link Given";

    private Dictionary<string, string> Regions = new() { ["Menu"] = "" };
    private Dictionary<string, LocationProgressType> LocationPriorities = [];
    private CodeBlockFactory CreateRegionCode = new();
    private List<RegionData> RegionDatas = [];

    public RegionFactory AddRegion(string region, string condition = "")
    {
        Regions.Add(region, condition);
        return this;
    }

    public RegionFactory AddRegions(string condition = "", params string[] regions)
    {
        foreach (var region in regions) AddRegion(region, condition);
        return this;
    }

    private RegionFactory AddIf<T>(string condition, TCodeBlockFactory<T> block) where T : TCodeBlockFactory<T>
    {
        if (condition is "")
        {
            CreateRegionCode.AddCode(block.GetText());
            return this;
        }

        CreateRegionCode.AddCode($"if {condition}:").AddCode(block.GetText(1)).AddNewLine();
        return this;
    }

    public RegionFactory AddConnection(string fromRegion, string toRegion, string rule = "", string connectionName = "",
        string condition = "")
    {
        var data = new RegionData(fromRegion, toRegion, rule, connectionName);
        RegionDatas.Add(data);
        return AddIf(condition, new CodeBlockFactory().AddCode(data.ToString()));
    }

    public RegionFactory AddConnectionCompiledRule(string fromRegion, string toRegion, string rule,
        string connectionName = "", string condition = "")
    {
        var data = new RegionData(
            fromRegion, toRegion, WorldFactory.GetRuleFactory().GenerateCompiledRule(rule), connectionName
        );

        RegionDatas.Add(new RegionData(fromRegion, toRegion, rule, connectionName));
        return AddIf(condition, new CodeBlockFactory().AddCode(data.ToString()));
    }

    public RegionFactory AddLocationPriority(string location, LocationProgressType progress)
    {
        if (progress is not LocationProgressType.Default) { LocationPriorities[location] = progress; }
        return this;
    }

    public RegionFactory AddLocationPriorities(LocationProgressType progress, params string[] locations)
    {
        if (progress is LocationProgressType.Default) return this;
        foreach (var location in locations) { LocationPriorities[location] = progress; }
        return this;
    }

    public RegionFactory AddLocation(LocationData location, string condition = "")
    {
        if (location.LocationProgress is not LocationProgressType.Default)
        {
            LocationPriorities[location.LocationName] = location.LocationProgress;
        }
        return AddIf(condition, new CodeBlockFactory().AddCode(location.ToString()));
    }

    public RegionFactory AddLocations(string condition = "", params LocationData[] locations)
    {
        foreach (var location in locations)
        {
            if (location.LocationProgress is not LocationProgressType.Default)
            {
                LocationPriorities[location.LocationName] = location.LocationProgress;
            }
        }

        return AddIf(
            condition,
            locations.Aggregate(new CodeBlockFactory(), (factory, location) => factory.AddCode(location.ToString()))
                     .AddNewLine()
        );
    }

    public RegionFactory AddEventLocation(EventLocationData location, string condition = "")
    {
        return AddIf(condition, new CodeBlockFactory().AddCode(location.ToString()));
    }

    public RegionFactory AddEventLocations(string condition = "", params EventLocationData[] locations)
    {
        return AddIf(
            condition,
            locations.Aggregate(new CodeBlockFactory(), (factory, location) => factory.AddCode(location.ToString()))
                     .AddNewLine()
        );
    }

    public RegionFactory AddLocationsFromList(string list, string getLocation = "location[0]",
        string getRegion = "location[1]",
        string condition = "", bool isCrucial = false)
    {
        return AddIf(
            condition, new ForLoopFactory("location", list)
               .AddCode(
                    new IfFactory($"{getRegion} in region_map")
                       .AddCode($"make_location(world, {getLocation}, {getRegion}, region_map, rule_map, {isCrucial})")
                )
        );
    }

    public RegionFactory AddEventLocationsFromList(string list, string getEventLocation = "f\"Event: {location[0]}\"",
        string item = "Event Item",
        string getLocation = "location[0]", string getRegion = "location[1]", string condition = "", bool isCrucial = false)
    {
        return AddIf(
            condition, new ForLoopFactory("location", list)
               .AddCode(
                    new IfFactory($"{getRegion} in region_map")
                       .AddCode(
                            $"make_event_location(world, {getEventLocation}, {getLocation}, {item}, None, {getRegion}, region_map, rule_map, {isCrucial})"
                        )
                )
        );
    }

    public RegionFactory InjectCodeIntoCreateRegions(Action<CodeBlockFactory> action)
    {
        action(CreateRegionCode);
        return this;
    }

    public void GenerateRegionFile(string fileOutput = "Regions.py", string imports = """
        from BaseClasses import Location, Region, Item, ItemClassification, LocationProgressType
        from .Locations import *
        from .Rules import *
        """)
    {
        var regionPy = new PythonFactory()
                      .AddObject(new Comment($"File is Auto-generated, see: [{RegionGeneratorLink}]"))
                      .AddImports(imports)
                      .AddObject(
                           new MappedVariable<string, string>(
                               "priority_map",
                               LocationPriorities.ToDictionary(
                                   kv => kv.Key.Surround('"'), kv => $"LocationProgressType.{PriorityMap[kv.Value]}"
                               )
                           )
                       );

        var regionGroups = Regions.GroupBy(kv => kv.Value).ToArray();
        var createRegions = new MethodFactory("gen_create_regions")
                           .AddParam("world")
                           .AddCode(new Variable("player", "world.player"))
                           .AddCode(new Variable("options", "world.options"))
                           .AddCode(new Variable("rule_map", "get_rule_map(player, options)")).AddNewLine()
                           .AddCode(
                                new MappedVariable<string, string>(
                                    "region_map",
                                    regionGroups.First(g => g.Key is "").Select(kv => kv.Key)
                                                .ToDictionary(
                                                     r => r.Surround("\""),
                                                     r => $"Region(\"{r}\", world.player, world.multiworld)"
                                                 )
                                )
                            ).AddNewLine();

        foreach (var group in regionGroups.Where(g => g.Key is not ""))
        {
            createRegions.AddCode(
                new IfFactory(group.Key).AddCode(
                    [
                        .. group.Select(kv
                            => $"region_map[\"{kv.Key}\"] = Region(\"{kv.Key}\", world.player, world.multiworld)"
                        ),
                    ]
                )
            );
        }

        createRegions.AddCode(CreateRegionCode).AddNewLine()
                     .AddCode(
                          new ForLoopFactory("region", "region_map.values()")
                             .AddCode("world.multiworld.regions.append(region)")
                      );

        regionPy.AddObject(createRegions)
                .AddObject(
                     new MethodFactory("connect_region")
                        .AddParams("from_region", "to_region", "region_map", "name", "rule", "is_connection_crucial")
                        .AddCode(
                             """
                             if from_region not in region_map:
                                if is_connection_crucial: throw_needed_region_error(from_region, f"connect_region, from: [{from_region}]")
                                return
                             if to_region not in region_map:
                                if is_connection_crucial: throw_needed_region_error(to_region, f"connect_region, to: [{to_region}]")
                                return
                             region_map[from_region].connect(region_map[to_region], name, rule = rule)
                             """
                         )
                 )
                .AddObject(
                     new MethodFactory("make_location")
                        .AddParams("world", "location_name", "region_name", "region_map", "rule_map", "is_location_crucial")
                        .AddCode(
                             """
                             loc = make_location_adv(world, location_name, location_name, world.location_name_to_id[location_name], region_name, region_map, rule_map, is_location_crucial)
                             if loc is not None: world.location_count += 1
                             return loc
                             """
                         )
                 )
                .AddObject(
                     new MethodFactory("make_event_location")
                        .AddParams(
                             "world", "location_name_a", "location_name_b", "item_name", "id", "region_name", "region_map", "rule_map", "is_location_crucial"
                         )
                        .AddCode(
                             """
                             location = make_location_adv(world, location_name_a, location_name_b, id, region_name, region_map, rule_map, is_location_crucial)
                             if location is None: return None
                             return location.place_locked_item(Item(item_name, ItemClassification.progression, None, world.player))
                             """
                         )
                 )
                .AddObject(
                     new MethodFactory("make_location_adv")
                        .AddParams("world", "location_name_a", "location_name_b", "id", "region_name", "region_map", "rule_map", "is_location_crucial")
                        .AddCode(
                             """
                             if region_name not in region_map:
                                if is_location_crucial: throw_needed_region_error(region_name, f"make_location_adv, [{location_name_a}]")
                                return None
                             
                             location = Location(world.player, location_name_a, id, region_map[region_name])
                             region_map[region_name].locations.append(location)

                             if location_name_b in rule_map:
                                location.access_rule = rule_map[location_name_b]

                             if location_name_a in priority_map:
                                location.progress_type = priority_map[location_name_a]

                             return location
                             """
                         )
                 )
                .AddObject(new MethodFactory("throw_needed_region_error")
                    .AddParams("region_name", "sender")
                          .AddCode("raise ValueError(f\"For an unknown reason the region, [{region_name}] was not added as a region, it is required for [{sender}]\")"));

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", regionPy.GetText());
    }
}

public readonly struct RegionData(string from, string to, string rule = "", string name = "", bool isConnectionCrucial = false)
{
    public readonly string From = from.Surround('"');
    public readonly string To = to.Surround('"');
    public readonly string Rule = rule;
    public readonly string Name = name is "" ? "" : name.Surround('"');
    public readonly bool IsConnectionCrucial = isConnectionCrucial;

    public override string ToString()
        => $"connect_region({From}, {To}, region_map, {(Name is "" ? "None" : Name)}, {(Rule is "" ? "None" : $"lambda state: {Rule}")}, {IsConnectionCrucial})";
}

public readonly struct EventLocationData(string region, string locationName, string lockedItemName,
    string inheritLocationRule = "", bool isLocCrucial = false)
{
    public readonly string Region = region.Surround('"');
    public readonly string LocationName = locationName.Surround('"');
    public readonly string InheritLocationRule = inheritLocationRule is "" ? "" : inheritLocationRule.Surround('"');
    public readonly string ItemName = lockedItemName.Surround('"');
    public readonly bool IsLocationCrucial = isLocCrucial;
    
    public override string ToString()
        => $"make_event_location(world, {LocationName}, {InheritLocationRule}, {ItemName}, None, {Region}, region_map, rule_map, {IsLocationCrucial})";
}

public readonly struct LocationData(string region, string locationName,
    bool isLocCrucial = false, RegionFactory.LocationProgressType locationProgress = RegionFactory.LocationProgressType.Default)
{
    public readonly string Region = region.Surround('"');
    public readonly string LocationName = locationName.Surround('"');
    public readonly RegionFactory.LocationProgressType LocationProgress = locationProgress;
    public readonly bool IsLocationCrucial = isLocCrucial;

    public override string ToString() => $"make_location(world, {LocationName}, {Region}, region_map, rule_map, {IsLocationCrucial})";
}