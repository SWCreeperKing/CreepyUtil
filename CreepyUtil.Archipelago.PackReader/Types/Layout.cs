using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.PackReader.Types;

public class Layout
{
    [JsonProperty("type")] private string _Type;
    [JsonProperty("background")] public string BackgroundColor; // #(AA)RRGGBB
    [JsonProperty("h_alignment")] private string _HorizontalAlignment;
    [JsonProperty("v_alignment")] private string _VerticalAlignment;
    [JsonProperty("dock")] private string _DockType;
    [JsonProperty("orientation")] private string _Orientation;
    [JsonProperty("max_height")] public int MaxHeight;
    [JsonProperty("max_width")] public int MaxWidth;
    [JsonProperty("margin")] private string _Margin;
    [JsonProperty("item_margin")] public string ItemMargin; // h,v 

    [JsonProperty("item_size")]
    public string ItemSize; // h,v | 3=default=32, 4=48, other TBD, 10+=size in pixels | idk what this means

    [JsonProperty("item_h_alignment")] private string _ItemHorizontalAlignment;
    [JsonProperty("item_v_alignment")] private string _ItemVerticalAlignment;
    [JsonProperty("dropshadow")] public bool DropShadow;
    [JsonProperty("text")] public string Text; // for Text type
    [JsonProperty("header_content")] public dynamic HeaderContent; //todo: WTF is this???
    [JsonProperty("content")] private dynamic _Content;
    [JsonProperty("key")] public string KeyReference; //todo: idk
    [JsonProperty("compact")] public bool Compact; //todo: idk

    public Enums.LayoutType Type => _Type.TextToLayoutType();
    public Enums.HorizontalAlignment HorizontalAlignment => _HorizontalAlignment.TextToHorizontalAlignment();
    public Enums.VerticalAlignment VerticalAlignment => _VerticalAlignment.TextToVerticalAlignment();
    public Enums.DockType Dock => _DockType.TextToDockType();
    public Enums.OrientationType Orientation => _Orientation.TextToOrientationType();

    public void GetMargins(out int left, out int top, out int right, out int bottom)
    {
        var index = -1;
        var split = _Margin.Split(',').Select(s => int.TryParse(s, out var i) ? i : 0).ToArray();
        left = split.Length > ++index ? split[index] : 0;
        top = split.Length > ++index ? split[index] : 0;
        right = split.Length > ++index ? split[index] : 0;
        bottom = split.Length > ++index ? split[index] : 0;
    }

    public Enums.HorizontalAlignment ItemHorizontalAlignment => _ItemHorizontalAlignment.TextToHorizontalAlignment();
    public Enums.VerticalAlignment ItemVerticalAlignment => _ItemVerticalAlignment.TextToVerticalAlignment();
    public Layout[] Content => Porter.DynamicToArr<Layout>(_Content);
}