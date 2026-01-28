using Archipelago.MultiClient.Net.Packets;

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
    private WorldFactory WorldFactory = worldFactory;
    public string RegionGeneratorLink = "No Link Given";

    private List<string> Regions = ["Menu"];
    private CodeBlockFactory CreateRegionCode = new();

    public RegionFactory AddRegion(string region)
    {
        Regions.Add(region);
        return this;
    }

    public RegionFactory AddRegions(params string[] regions)
    {
        Regions.AddRange(regions);
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

    public RegionFactory AddConnection(string fromRegion, string toRegion, string rule = "", string connectionName = "", string condition = "")
    {
        return AddIf(condition, new CodeBlockFactory().AddCode(new RegionData(fromRegion, toRegion, rule, connectionName).ToString()));
    }

    public RegionFactory AddConnectionCompiledRule(string fromRegion, string toRegion, string rule, string connectionName = "", string condition = "")
    {
        return AddIf(condition, new CodeBlockFactory().AddCode(new RegionData(fromRegion, toRegion, WorldFactory.GetRuleFactory().GenerateCompiledRule(rule), connectionName).ToString()));
    }

    public RegionFactory AddLocation(LocationData location, string condition = "")
    {
        return AddIf(condition, new CodeBlockFactory().AddCode(location.ToString()));
    }

    public RegionFactory AddLocations(string condition = "", params LocationData[] locations)
    {
        return AddIf(
            condition, locations.Aggregate(
                new CodeBlockFactory(), (factory, location)
                    => factory.AddCode(location.ToString())
            ).AddNewLine()
        );
    }

    public RegionFactory AddEventLocation(EventLocationData location, string condition = "")
    {
        return AddIf(condition, new CodeBlockFactory().AddCode(location.ToString()));
    }

    public RegionFactory AddEventLocations(string condition = "", params EventLocationData[] locations)
    {
        return AddIf(
            condition, locations.Aggregate(
                new CodeBlockFactory(), (factory, location)
                    => factory.AddCode(location.ToString())
            ).AddNewLine()
        );
    }

    public RegionFactory AddLocationsFromList(string list, string getLocation = "location[0]", string getRegion = "region_map[location[1]]", string condition = "")
    {
        return AddIf(
            condition, new ForLoopFactory("location", list)
               .AddCode($"make_location(world, {getLocation}, {getRegion}, rule_map)")
        );
    }

    public RegionFactory AddEventLocationsFromList(string list, string getEventLocation = "f\"Event: {location[0]}\"", string item = "Event Item", string getLocation = "location[0]", string getRegion = "region_map[location[1]]", string condition = "")
    {
        return AddIf(
            condition, new ForLoopFactory("location", list)
               .AddCode($"make_event_location(world, {getEventLocation}, {getLocation}, {item}, None, {getRegion}, rule_map)")
        );
    }

    public RegionFactory InjectCodeIntoCreateRegions(Action<CodeBlockFactory> action)
    {
        action(CreateRegionCode);
        return this;
    }

    public void GenerateRegionFile(
        string fileOutput = "Regions.py", string imports = """
                                                           from BaseClasses import Location, Region, Item, ItemClassification
                                                           from .Locations import *
                                                           from .Rules import *
                                                           """
    )
    {
        var regionPy = new PythonFactory()
                      .AddObject(new Comment($"File is Auto-generated, see: [{RegionGeneratorLink}]"))
                      .AddImports(imports);

        var createRegions = new MethodFactory("gen_create_regions")
                           .AddParam("world")
                           .AddCode(new Variable("player", "world.player"))
                           .AddCode(new Variable("rule_map", "get_rule_map(world.player)")).AddNewLine()
                           .AddCode(new MappedVariable<string, string>("region_map", Regions.ToDictionary(r => r.Surround("\""), r => $"Region(\"{r}\", world.player, world.multiworld)"))).AddNewLine()
                           .AddCode(CreateRegionCode).AddNewLine()
                           .AddCode(
                                new ForLoopFactory("region", "region_map.values()")
                                   .AddCode("world.multiworld.regions.append(region)")
                            );

        regionPy.AddObject(createRegions)
                .AddObject(
                     new MethodFactory("make_location")
                        .AddParams("world", "location_name", "region", "rule_map")
                        .AddCode("return make_location_adv(world, location_name, location_name, world.location_name_to_id[location_name], region, rule_map)")
                 )
                .AddObject(
                     new MethodFactory("make_event_location")
                        .AddParams("world", "location_name_a", "location_name_b", "item_name", "id", "region", "rule_map")
                        .AddCode(
                             """
                             location = make_location_adv(world, location_name_a, location_name_b, id, region, rule_map)
                             location.place_locked_item(Item(item_name, ItemClassification.progression, None, world.player))
                             """
                         )
                 )
                .AddObject(
                     new MethodFactory("make_location_adv")
                        .AddParams("world", "location_name_a", "location_name_b", "id", "region", "rule_map")
                        .AddCode(
                             """
                             location = Location(world.player, location_name_a, id, region)
                             region.locations.append(location)

                             if location_name_b in rule_map:
                                  location.access_rule = rule_map[location_name_b]

                             world.location_count += 1
                             return location
                             """
                         )
                 );

        File.WriteAllText($"{worldFactory.OutputDirectory}{fileOutput}", regionPy.GetText());
    }
}

public readonly struct RegionData(string from, string to, string rule = "", string name = "")
{
    public readonly string From = from.Surround('"');
    public readonly string To = to.Surround('"');
    public readonly string Rule = rule;
    public readonly string Name = name is "" ? "" : name.Surround('"');

    public override string ToString()
    {
        return Rule is not ""
            ? $"region_map[{From}].connect(region_map[{To}], {(Name is "" ? "rule =" : $"{Name},")} lambda state: {Rule})"
            : $"region_map[{From}].connect(region_map[{To}]{(Name is "" ? "" : $", {Name}")})";
    }
}

public readonly struct EventLocationData(string region, string locationName, string lockedItemName, string inheritLocationRule = "")
{
    public readonly string Region = region.Surround('"');
    public readonly string LocationName = locationName.Surround('"');
    public readonly string InheritLocationRule = inheritLocationRule is "" ? "" : inheritLocationRule.Surround('"');
    public readonly string ItemName = lockedItemName.Surround('"');

    public override string ToString() => $"make_event_location(world, {LocationName}, {InheritLocationRule}, {ItemName}, None, region_map[{Region}], rule_map)";
}

public readonly struct LocationData(string region, string locationName)
{
    public readonly string Region = region.Surround('"');
    public readonly string LocationName = locationName.Surround('"');

    public override string ToString() => $"make_location(world, {LocationName}, region_map[{Region}], rule_map)";
}