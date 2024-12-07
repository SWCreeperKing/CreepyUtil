using System.Globalization;
using static CreepyUtil.ClrCnsl.ColorExt;

namespace CreepyUtil.ClrCnsl;

public readonly record struct Color
{
    private static readonly Dictionary<string, Color> ParseCache = [];
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color White = new(255, 255, 255);

    public readonly byte R;
    public readonly byte G;
    public readonly byte B;

    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public Color(string text)
    {
        if (ParseCache.TryGetValue(text, out this)) return;
        if (ColorDict.TryGetValue(text, out var hex))
        {
            this = HexToColor(hex);
            return;
        }

        if (IsHexColor(text))
        {
            this = HexToColor(text);
            return;
        }

        if (text.Contains(','))
        {
            var split = text.Split(',');
            if (split.Length != 3)
            {
                this = White;
                return;
            }

            if (!byte.TryParse(split[0], out R)) R = 255;
            if (!byte.TryParse(split[1], out G)) G = 255;
            if (!byte.TryParse(split[2], out B)) B = 255;
            ParseCache[text] = this;
            return;
        }

        Console.WriteLine(
            $"Failed to parse: [{text}] | not in chache, not in color dictionary, not hex, not ###,###,###");
        this = White;
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
        return White;
    }

    public static implicit operator Color(string text) => new(text);
    public static implicit operator string(Color color) => color.ToString();

    public override string ToString() { return $"{R},{G},{B}"; }
}