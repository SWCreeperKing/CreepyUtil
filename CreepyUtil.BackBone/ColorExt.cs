using System.Globalization;
using System.Numerics;
using Raylib_cs;

namespace CreepyUtil.BackBone;

public static class ColorExt
{
    private static readonly Dictionary<string, Color> ParseCache = [];
    private static readonly Dictionary<Color, Vector3> ParseCacheV3 = [];
    private static readonly Dictionary<Color, Vector4> ParseCacheV4 = [];
    
    public static Vector4 ToV4(this Color color)
    {
        if (ParseCacheV4.TryGetValue(color, out var v4)) return v4;
        return ParseCacheV4[color] = new Vector4(color.R, color.G, color.B, color.A) / 255f;
    }

    public static Vector3 ToV3(this Color color)
    {
        if (ParseCacheV3.TryGetValue(color, out var v3)) return v3;
        return ParseCacheV3[color] = new Vector3(color.R, color.G, color.B) / 255f;
    }

    public static Color ToColor(this string text)
    {
        if (ParseCache.TryGetValue(text, out var cachedColor)) return cachedColor;
        if (ColorDict.TryGetValue(text, out var hex))
        {
            return HexToColor(hex);
        }

        if (IsHexColor(text))
        {
            return HexToColor(hex);
        }

        if (text.Contains(','))
        {
            var split = text.Split(',');
            if (split.Length != 3)
            {
                return Color.White;
            }

            if (!byte.TryParse(split[0], out var r)) r = 255;
            if (!byte.TryParse(split[1], out var g)) g = 255;
            if (!byte.TryParse(split[2], out var b)) b = 255;
            return ParseCache[text] = new Color(r, g, b);
        }

        Console.WriteLine(
            $"Failed to parse: [{text}] | not in cache, not in color dictionary, not hex, not ###,###,###");
        return Color.White;
    }

    public static bool IsHexColor(string hex)
    {
        return hex.Length == 6 && hex.All(c => c is >= 'a' and <= 'f' or >= '0' and <= '9' or >= 'A' and <= 'F');
    }

    public static Color HexToColor(string hex)
    {
        if (IsHexColor(hex))
        {
            var r = byte.Parse(hex[..2], NumberStyles.HexNumber);
            var g = byte.Parse(hex[2..4], NumberStyles.HexNumber);
            var b = byte.Parse(hex[4..6], NumberStyles.HexNumber);
            Color color = new(r, g, b);
            ParseCache[hex] = color;
            return color;
        }

        Console.WriteLine($"Failed to parse, [{hex}] is not hex");
        return Color.White;
    }

    public static readonly Dictionary<string, string> ColorDict = new()
    {
        ["aliceblue"] = "F0F8FF",
        ["lightsalmon"] = "FFA07A",
        ["antiquewhite"] = "FAEBD7",
        ["lightseagreen"] = "20B2AA",
        ["aqua"] = "00FFFF",
        ["lightskyblue"] = "87CEFA",
        ["aquamarine"] = "7FFFD4",
        ["lightslategray"] = "778899",
        ["w"] = "FFFFFF",
        ["lightslategrey"] = "778899",
        ["azure"] = "F0FFFF",
        ["lightsteelblue"] = "B0C4DE",
        ["beige"] = "F5F5DC",
        ["lightyellow"] = "FFFFE0",
        ["bisque"] = "FFE4C4",
        ["lime"] = "00FF00",
        ["black"] = "000000",
        ["limegreen"] = "32CD32",
        ["blanchedalmond"] = "FFEBCD",
        ["linen"] = "FAF0E6",
        ["blue"] = "3B78FF",
        ["magenta"] = "B4009E",
        ["blueviolet"] = "8A2BE2",
        ["maroon"] = "800000",
        ["brown"] = "A52A2A",
        ["mediumaquamarine"] = "66CDAA",
        ["burlywood"] = "DEB887",
        ["mediumblue"] = "0000CD",
        ["cadetblue"] = "5F9EA0",
        ["mediumorchid"] = "BA55D3",
        ["chartreuse"] = "7FFF00",
        ["mediumpurple"] = "9370DB",
        ["chocolate"] = "D2691E",
        ["mediumseagreen"] = "3CB371",
        ["coral"] = "FF7F50",
        ["mediumslateblue"] = "7B68EE",
        ["cornflowerblue"] = "6495ED",
        ["mediumspringgreen"] = "00FA9A",
        ["cornsilk"] = "FFF8DC",
        ["mediumturquoise"] = "48D1CC",
        ["crimson"] = "DC143C",
        ["mediumvioletred"] = "C71585",
        ["cyan"] = "61D6D6",
        ["midnightblue"] = "191970",
        ["darkblue"] = "0037DA",
        ["mintcream"] = "F5FFFA",
        ["darkcyan"] = "3A96DD",
        ["darkaqua"] = "3A96DD",
        ["mistyrose"] = "FFE4E1",
        ["darkgoldenrod"] = "B8860B",
        ["moccasin"] = "FFE4B5",
        ["darkgray"] = "767676",
        ["darkgrey"] = "767676",
        ["navajowhite"] = "FFDEAD",
        ["darkgreen"] = "13A10E",
        ["navy"] = "000080",
        ["darkkhaki"] = "BDB76B",
        ["oldlace"] = "FDF5E6",
        ["darkmagenta"] = "881798",
        ["darkfuchsia"] = "881798",
        ["olive"] = "808000",
        ["darkolivegreen"] = "556B2F",
        ["olivedrab"] = "6B8E23",
        ["darkorange"] = "FF8C00",
        ["orange"] = "FFA500",
        ["darkorchid"] = "9932CC",
        ["orangered"] = "FF4500",
        ["darkred"] = "C50F1F",
        ["orchid"] = "DA70D6",
        ["darksalmon"] = "E9967A",
        ["palegoldenrod"] = "EEE8AA",
        ["darkseagreen"] = "8FBC8F",
        ["palegreen"] = "98FB98",
        ["darkslateblue"] = "483D8B",
        ["paleturquoise"] = "AFEEEE",
        ["darkslategray"] = "2F4F4F",
        ["darkyellow"] = "C19C00",
        ["darkslategrey"] = "2F4F4F",
        ["palevioletred"] = "DB7093",
        ["darkturquoise"] = "00CED1",
        ["papayawhip"] = "FFEFD5",
        ["darkviolet"] = "9400D3",
        ["peachpuff"] = "FFDAB9",
        ["deeppink"] = "FF1493",
        ["peru"] = "CD853F",
        ["deepskyblue"] = "00BFFF",
        ["pink"] = "FFC0CB",
        ["dimgray"] = "696969",
        ["dimgrey"] = "696969",
        ["plum"] = "DDA0DD",
        ["dodgerblue"] = "1E90FF",
        ["powderblue"] = "B0E0E6",
        ["firebrick"] = "B22222",
        ["purple"] = "800080",
        ["floralwhite"] = "FFFAF0",
        ["red"] = "E74856",
        ["forestgreen"] = "228B22",
        ["rosybrown"] = "BC8F8F",
        ["fuchsia"] = "B4009E",
        ["royalblue"] = "4169E1",
        ["gainsboro"] = "DCDCDC",
        ["saddlebrown"] = "8B4513",
        ["ghostwhite"] = "F8F8FF",
        ["salmon"] = "FA8072",
        ["gold"] = "FFD700",
        ["sandybrown"] = "FAA460",
        ["goldenrod"] = "DAA520",
        ["seagreen"] = "2E8B57",
        ["gray"] = "CCCCCC",
        ["grey"] = "CCCCCC",
        ["seashell"] = "FFF5EE",
        ["green"] = "16C60C",
        ["sienna"] = "A0522D",
        ["greenyellow"] = "ADFF2F",
        ["silver"] = "C0C0C0",
        ["honeydew"] = "F0FFF0",
        ["skyblue"] = "87CEEB",
        ["hotpink"] = "FF69B4",
        ["slateblue"] = "6A5ACD",
        ["indianred"] = "CD5C5C",
        ["slategray"] = "708090",
        ["slategrey"] = "708090",
        ["indigo"] = "4B0082",
        ["snow"] = "FFFAFA",
        ["ivory"] = "FFFFF0",
        ["springgreen"] = "00FF7F",
        ["khaki"] = "F0E68C",
        ["steelblue"] = "4682B4",
        ["lavender"] = "E6E6FA",
        ["tan"] = "D2B48C",
        ["lavenderblush"] = "FFF0F5",
        ["teal"] = "61D6D6",
        ["lawngreen"] = "7CFC00",
        ["thistle"] = "D8BFD8",
        ["lemonchiffon"] = "FFFACD",
        ["tomato"] = "FF6347",
        ["lightblue"] = "ADD8E6",
        ["turquoise"] = "40E0D0",
        ["lightcoral"] = "F08080",
        ["violet"] = "EE82EE",
        ["lightcyan"] = "E0FFFF",
        ["lightaqua"] = "E0FFFF",
        ["wheat"] = "F5DEB3",
        ["lightgoldenrodyellow"] = "FAFAD2",
        ["white"] = "FFFFFF",
        ["lightgreen"] = "90EE90",
        ["whitesmoke"] = "F5F5F5",
        ["lightgray"] = "D3D3D3",
        ["lightgrey"] = "D3D3D3",
        ["yellow"] = "F9F1A5",
        ["lightpink"] = "FFB6C1",
        ["yellowgreen"] = "9ACD32"
    };
}