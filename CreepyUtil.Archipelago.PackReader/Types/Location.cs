using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.PackReader.Types;

//https://github.com/black-sliver/PopTracker/blob/master/doc/PACKS.md#locations
public class Location
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("short_name")] public string ShortName;
    [JsonProperty("access_rules")] public dynamic _AccessRules;
    [JsonProperty("visibility_rules")] public dynamic _VisibilityRules;
    [JsonProperty("chest_unopened_img")] public string UnopenedImage;
    [JsonProperty("chest_opened_img")] public string OpenedImage;
    [JsonProperty("overlay_background")] public string OverlayBackground; // #(AA)RRGGBB
    [JsonProperty("color")] public string Color;
    [JsonProperty("parent")] public string Parent;
    [JsonProperty("children")] public Location[] Locations;
    [JsonProperty("map_locations")] public MapLocation[] MapLocations;
    [JsonProperty("sections")] public Section[] Sections;

    public string[] AccessRules => Porter.DynamicToArr<string>(_AccessRules);
    public string[] VisibilityRules => Porter.DynamicToArr<string>(_VisibilityRules);
}

public struct MapLocation
{
    [JsonProperty("map")] public string MapName;
    [JsonProperty("x")] public float X;
    [JsonProperty("y")] public float Y;
    [JsonProperty("size")] public float Size;
    [JsonProperty("border_thickness")] public float BorderThickness;
    [JsonProperty("shape")] private string _Shape;

    [JsonProperty("restrict_visibility_rules")]
    public string[] RestrictedVisibilityRules;
    
    [JsonProperty("force_invisibility_rules")]
    public string[] ForcedInvisibilityRules;
    
    public Enums.Shape LocationShape => _Shape?.TextToShape() ?? Enums.Shape.Rect;
}

public struct Section
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("clear_as_group")] public bool ClearAsGroup;
    [JsonProperty("chest_unopened_img")] public string UnopenedImage;
    [JsonProperty("chest_opened_img")] public string OpenedImage;
    [JsonProperty("item_count")] public int ItemCount;
    [JsonProperty("hosted_item")] public string HostedItem;
    [JsonProperty("access_rules")] private dynamic _AccessRules;
    [JsonProperty("visibility_rules")] private dynamic _VisibilityRules;
    [JsonProperty("ref")] public string Reference;
    
    public string[] AccessRules => Porter.DynamicToArr<string>(_AccessRules);
    public string[] VisibilityRules => Porter.DynamicToArr<string>(_VisibilityRules);
}