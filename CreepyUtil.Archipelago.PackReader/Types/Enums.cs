namespace CreepyUtil.Archipelago.PackReader.Types;

public static class Enums
{
    public enum Shape
    {
        Rect,
        Diamond,
        Trapezoid
    }

    public enum LayoutType
    {
        Container,
        Dock,
        Array,
        Tabbed,
        Group,
        Item,
        ItemGrid,
        Map,
        Layout,
        RecentPins,
        Text,
        Canvas
    }

    public enum HorizontalAlignment
    {
        Left,
        Right,
        Center,
        Stretch
    }

    public enum VerticalAlignment
    {
        Top,
        Bottom,
        Center,
        Stretch
    }

    public enum DockType
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public enum OrientationType
    {
        Horizontal,
        Vertical
    }

    public enum AutoTrackingBackendName
    {
        SNES,
        UAT,
        AP
    }
    
    public enum AutoTrackingState
    {
        Unavailable = -1,
        Disabled = 0,
        Disconnected,
        SocketConnectedNotReady,
        GameConnectedReady
    }
    
    internal static Shape TextToShape(this string text)
        => text.ToLower() switch
        {
            "rect" or "rectangle" => Shape.Rect,
            "diamond" => Shape.Diamond,
            "trapezoid" => Shape.Trapezoid,
            _ => throw new ArgumentOutOfRangeException()
        };

    internal static LayoutType TextToLayoutType(this string text)
        => text.ToLower() switch
        {
            "container" => LayoutType.Container,
            "dock" => LayoutType.Dock,
            "array" => LayoutType.Array,
            "tabbed" => LayoutType.Tabbed,
            "group" => LayoutType.Group,
            "item" => LayoutType.Item,
            "itemgrid" => LayoutType.ItemGrid,
            "map" => LayoutType.Map,
            "layout" => LayoutType.Layout,
            "recentpins" => LayoutType.RecentPins,
            "text" => LayoutType.Text,
            "canvas" => LayoutType.Canvas
        };

    internal static HorizontalAlignment TextToHorizontalAlignment(this string text)
        => text.ToLower() switch
        {
            "left" => HorizontalAlignment.Left,
            "right" => HorizontalAlignment.Right,
            "center" => HorizontalAlignment.Center,
            "stretch" => HorizontalAlignment.Stretch
        };

    internal static VerticalAlignment TextToVerticalAlignment(this string text)
        => text.ToLower() switch
        {
            "top" => VerticalAlignment.Top,
            "bottom" => VerticalAlignment.Bottom,
            "center" => VerticalAlignment.Center,
            "stretch" => VerticalAlignment.Stretch
        };

    internal static DockType TextToDockType(this string text)
        => text.ToLower() switch
        {
            "top" => DockType.Top,
            "bottom" => DockType.Bottom,
            "left" => DockType.Left,
            "right" => DockType.Right
        };

    internal static OrientationType TextToOrientationType(this string text)
        => text.ToLower() switch
        {
            "horizontal" => OrientationType.Horizontal,
            "vertical" => OrientationType.Vertical
        };

    internal static AutoTrackingBackendName TextToAutoTrackingBackendName(this string text)
        => text.ToLower() switch
        {
            "snes" => AutoTrackingBackendName.SNES,
            "uat" => AutoTrackingBackendName.UAT,
            "ap" => AutoTrackingBackendName.AP
        };

}