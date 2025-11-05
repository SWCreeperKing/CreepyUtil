using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreepyUtil.Archipelago.PackReader.Types;

public struct Manifest
{
    [JsonProperty("name")] public string Name;
    [JsonProperty("game_name")] public string GameName;
    [JsonProperty("author")] public string Author;
    [JsonProperty("package_uid")] public string PackUid;
    [JsonProperty("package_version")] public string PackVersion;
    [JsonProperty("versions_url")] public string PackVersionUrl;
    [JsonProperty("variants")] public Dictionary<string, Variant> PackVariants;
    [JsonProperty("platform")] public string Platform;
    [JsonProperty("platform_override")] public string PlatformOverride;

    [JsonProperty("min_poptracker_version")]
    public string MinTrackerVersion;

    [JsonProperty("target_poptracker_version")]
    public string TargetTrackerVersion;
}

public struct Variant
{
    [JsonProperty("display_name")] public string DisplayName;
    [JsonProperty("flags")] private dynamic _Flags;

    public string[] Flags => Porter.DynamicToArr<string>(_Flags);
}