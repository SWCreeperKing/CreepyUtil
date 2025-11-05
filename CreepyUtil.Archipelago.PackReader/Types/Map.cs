using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.PackReader.Types;

public class Map
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("img")] public string Image;
    [JsonProperty("location_size")] public int LocationSize = 24;

    [JsonProperty("location_border_thickness")]
    public int LocationBorderSize = 2;

    [JsonProperty("location_shape")] private string _LocationShape;

    public Enums.Shape LocationShape => _LocationShape?.TextToShape() ?? Enums.Shape.Rect;
}