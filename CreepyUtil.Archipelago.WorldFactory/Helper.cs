namespace CreepyUtil.Archipelago.WorldFactory;

public static class Helper
{
    public static string Repeat(this char text, int count) => string.Join("", Enumerable.Repeat(text, count));
    public static string Repeat(this string text, int count) => string.Join("", Enumerable.Repeat(text, count));
    public static string[] ShredString(this string text) => text.Replace("\r", "").Split('\n');
    public static string Surround(this string text, char surrounding) => $"{surrounding}{text}{surrounding}";
    public static string Surround(this string text, string surrounding) => $"{surrounding}{text}{surrounding}";

    public static string FormatStringForOptionsVar(this string text, bool isClassVar = false, bool stringify = false)
    {
        var txt = (isClassVar ? text.Replace(" ", "") : text.ToLower().Replace(" ", "_")).Replace("'", "_");
        return stringify ? txt.Surround('"') : txt;
    }
}