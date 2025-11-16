using CreepyUtil.Archipelago.PackReader.Types;
using Newtonsoft.Json;
using NLua;

namespace CreepyUtil.Archipelago.PackReader.Bridge;

public partial class Tracker(Pack pack, string variant)
{
    public string ActiveVariantUID = variant;
    public bool BulkUpdate = false;
    public bool AllowDeferredLogicUpdate = false;

    [LuaHide] public List<Item> ItemsList = [];
    [LuaHide] public List<Map> MapsList = [];
    [LuaHide] public List<Location> LocationsList = [];
    [LuaHide] public Dictionary<string, Layout> Layouts = [];
    [LuaHide] public Pack Pack = pack;

    public bool AddItems(string jsonFileName)
    {
        try
        {
            ItemsList.AddRange(JsonConvert.DeserializeObject<Item[]>(Pack.ReadFile(jsonFileName))!);
            return true;
        }
        catch (Exception e)
        {
            Porter.LogInfo($"error for: [{jsonFileName}]\n{e}");
            return false;
        }
    }

    public bool AddMaps(string jsonFileName)
    {
        try
        {
            MapsList.AddRange(JsonConvert.DeserializeObject<Map[]>(Pack.ReadFile(jsonFileName))!);
            return true;
        }
        catch (Exception e)
        {
            Porter.LogInfo($"error for: [{jsonFileName}]\n{e}");
            return false;
        }
    }

    public bool AddLocations(string jsonFileName)
    {
        try
        {
            LocationsList.AddRange(JsonConvert.DeserializeObject<Location[]>(Pack.ReadFile(jsonFileName))!);
            return true;
        }
        catch (Exception e)
        {
            Porter.LogInfo($"error for: [{jsonFileName}]\n{e}");
            return false;
        }
    }
    
    public bool AddLayouts(string jsonFileName)
    {
        try
        {
            foreach (var (key, value) in JsonConvert.DeserializeObject<Dictionary<string, Layout>>(
                         Pack.ReadFile(jsonFileName))!)
            {
                Layouts[key] = value;
            }

            return true;
        }
        catch (Exception e)
        {
            Porter.LogInfo($"error for: [{jsonFileName}]\n{e}");
            return false;
        }
    }

    //TODO: lack of understanding
    public int ProviderCountForCode(string code) { throw new NotImplementedException(); }

    //TODO: lack of understanding
    public object FindObjectForCode(string code) { throw new NotImplementedException(); }
    
    //TODO: lack of understanding
    public void UiHint(string name, string value) { throw new NotImplementedException(); }
}