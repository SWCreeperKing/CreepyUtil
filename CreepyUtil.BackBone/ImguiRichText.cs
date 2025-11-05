using System.Text.RegularExpressions;
using ImGuiNET;

namespace CreepyUtil.BackBone;

public static partial class RlImgui
{
    public static readonly Regex RegMatch = new(@"\[#([a-zA-Z0-9]+?|\d{1,3},\d{1,3},\d{1,3})\]",
        RegexOptions.Compiled);
    
    private static readonly Dictionary<string, string> CleanColorCache = [];

    public static void RichText(string text)
    {
        var depth = 0;
        var index = 0;
        Span<char> txt = text.ToCharArray();

        while (RegMatch.IsMatch(txt, index))
        {
            var match = RegMatch.Match(text, index);
            var matchValue = match.Value;
            var nIndex = text.IndexOf(matchValue, index, StringComparison.Ordinal);

            if (index > 0)
            {
                ImGui.SameLine(0, 0);
            }
            ImGui.Text(txt[index..nIndex].ToString());

            var rawValue = match.Groups[1].Value.ToLower();
            if (rawValue == "r")
            {
                if (depth > 0)
                {
                    ImGui.PopStyleColor();
                    depth--;
                }
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, rawValue.ToColor().ToV4());
                depth++;
            }

            index = nIndex + matchValue.Length;
        }

        if (index < txt.Length)
        {
            ImGui.SameLine(0, 0);
            ImGui.Text(txt[index..].ToString());
            ImGui.NewLine();
        }

        for (var i = depth; i > 0; i--)
        {
            ImGui.PopStyleColor();
        }
    }
    
    public static string CleanColors(string? text)
    {
        text ??= "";
        if (CleanColorCache.TryGetValue(text, out var clean)) return clean;
        
        var s = CleanColorCache[text] = RegMatch.Replace(text, string.Empty);
        return s;
    }
}