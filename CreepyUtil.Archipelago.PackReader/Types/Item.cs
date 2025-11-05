using Newtonsoft.Json;

namespace CreepyUtil.Archipelago.PackReader.Types;

//https://github.com/black-sliver/PopTracker/blob/master/doc/PACKS.md#items
public class Item
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("type")] public string Type;

    #region Type = "progressive"

    [JsonProperty("allow_disabled")] public bool AllowDisabled = true;
    [JsonProperty("loop")] public bool Loop = false;
    [JsonProperty("initial_stage_idx")] public int InitialStageIndex = 0;
    [JsonProperty("stages")] public Stage[] Stages;

    #endregion

    #region Type = "toggle"/"static"

    [JsonProperty("img")] public string Image;
    [JsonProperty("img_mods")] public string ImageMods;
    [JsonProperty("codes")] public string Codes;
    [JsonProperty("disabled_img")] public string DisabledImage;
    [JsonProperty("disabled_img_mods")] public string DisabledImageMods;
    [JsonProperty("initial_active_state")] public bool InitialActiveState;

    #endregion

    #region Type = "composite_toggle"

    [JsonProperty("images")] public CompositeToggle[] Images;

    #endregion

    #region Type = "toggle_badged"

    [JsonProperty("base_item")] public string BaseItem;

    #endregion
}

public struct Stage
{
    [JsonProperty("img")] public string Image;
    [JsonProperty("disabled_img")] public string DisabledImage;
    [JsonProperty("codes")] public string Codes;
    [JsonProperty("secondary_codes")] public string SecondaryCodes;
    [JsonProperty("inherit_codes")] public bool InheritCodes;
    [JsonProperty("img_mods")] public string ImageMods;
    [JsonProperty("disabled_img_mods")] public string DisabledImageMods;
    [JsonProperty("name")] public string Name;
}

public struct CompositeToggle
{
    [JsonProperty("left")] public bool Left;
    [JsonProperty("right")] public bool Right;
    [JsonProperty("img")] public string Image;
    [JsonProperty("img_mods")] public string ImageMods;
}