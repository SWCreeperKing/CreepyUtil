using CreepyUtil.Archipelago.PackReader.Bridge;
using NLua;

namespace CreepyUtil.Archipelago.PackReader.Types;

public class LuaItem
{
    public string Name;
    public ImageRef? Icon = null;
    public LuaTable? ItemState = null;
    public LuaTable Owner;
    public string Type = "custom";
    public LuaFunction? CanProvideCodeFunc = null; // Func<self, string code, bool>
    public LuaFunction? ProvidesCodeFunc = null; // Func<self, string code, bool>
    public LuaFunction? AdvanceToCodeFunc = null; // Action<self, string code> (not in use?)
    public LuaFunction? OnLeftClickFunc = null; // Action<self>
    public LuaFunction? OnRightClickFunc = null; // Action<self>
    public LuaFunction? OnMiddleClickFunc = null; // Action<self>
    public LuaFunction? SaveFunc = null; // Func<self, object>
    public LuaFunction? LoadFunc = null; // Action<self, object data>
    public LuaFunction? PropertyChangedFunc = null; // Action<self, string key, object value>

    public void Set(string key, object value)
    {
        throw new NotImplementedException();
    }
    
    public object Set(string key)
    {
        throw new NotImplementedException();
    }

    public void SetOverlay(string text)
    {
        throw new NotImplementedException();
    }
    
    public void SetOverlayColor(string color) // "#rgb", "#rrggbb", "#argb", "#aarrggbb" or "" (default)
    {
        throw new NotImplementedException();
    }
    
    public void SetOverlayBackground(string background) // "#rgb", "#rrggbb", "#argb", "#aarrggbb" or "" (default)
    {
        throw new NotImplementedException();
    }

    public void SetOverlayFontSize(int fontSize)
    {
        throw new NotImplementedException();
    }
}