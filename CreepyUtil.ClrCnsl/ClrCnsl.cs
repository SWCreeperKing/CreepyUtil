using System.Text;
using System.Text.RegularExpressions;
using static System.ConsoleKey;
using static CreepyUtil.ClrCnsl.AsciiEffects;

namespace CreepyUtil.ClrCnsl;

public static class ClrCnsl
{
    // public const char Block = '\u2588';
    public const int ListLeng = 11;

    public static readonly Regex RegMatch = new(@"\[(?:(#|!)([a-zA-Z]+?|\d{1,3},\d{1,3},\d{1,3})|(@)(\w+?))\]",
        RegexOptions.Compiled);

    public static bool UseAscii;

    private static readonly string[] SourceArray = ["^", "v"];

    public static void EnableAscii()
    {
        Console.OutputEncoding = Encoding.ASCII;
        UseAscii = true;
    }

    public static int ListView(int selection = 0, params string[] options)
    {
        var amounts = options.Select(s => CleanColors(s).Length).ToArray();
        var maxSpace = amounts.Max() + 2;
        var top = GetCursor().Y;
        var selected = selection;
        var isMore = options.Length > ListLeng;

        var formattedOptions = options.Select(SpaceOut).ToArray();
        var moreLeng = SourceArray.Select(SpaceOut).ToArray();

        while (true)
        {
            Console.SetCursorPosition(0, top);

            var tempSelected = DisplayUpdateListView(isMore, formattedOptions, moreLeng, selected, options);
            if (tempSelected == -1) return selected;
            selected = tempSelected;
        }

        string SpaceOut(string s)
        {
            var len = CleanColors(s).Length;
            var remainder = maxSpace - len;
            var halfRemainder = remainder / 2f;
            var left = (int)Math.Floor(halfRemainder) + s.Length;
            var right = (int)Math.Ceiling(halfRemainder);
            return s.PadLeft(left).PadRight(left + right);
        }
    }

    private static int DisplayUpdateListView(bool isMore, string[] formattedOptions, string[] moreLeng, int selected,
        string[] options)
    {
        if (isMore) WriteLine($"[#yellow]{moreLeng[0]}");

        for (var i = 0; i < Math.Min(formattedOptions.Length, ListLeng); i++)
        {
            var rI = i + Math.Max(0,
                Math.Min(formattedOptions.Length - ListLeng, selected + 1 - (int)Math.Ceiling(ListLeng / 2f)));
            var isS = selected == rI;
            WriteLine(
                $"{(isS ? "[#green] >[!darkgray][#cyan]" : "  [#blue]")}{formattedOptions[rI]}{(isS ? "[#green][!black]< " : "  ")}");
        }

        if (isMore) WriteLine($"[#yellow]{moreLeng[1]}");

        switch (GetKey())
        {
            case W or UpArrow:
                selected--;
                if (selected < 0) return options.Length - 1;
                break;
            case S or DownArrow:
                selected++;
                if (selected >= options.Length) return 0;
                break;
            case Enter or Spacebar:
                return -1;
        }

        return selected;
    }

    public static void Write(string? text)
    {
        if (text is null) return;

        if (!RegMatch.IsMatch(text))
        {
            Console.Write(text);
            return;
        }

        if (UseAscii)
        {
            WriteAscii(text);
            return;
        }

        Stack<ConsoleColor> foregroundColorStack = new();
        Stack<ConsoleColor> backgroundColorStack = new();
        foregroundColorStack.Push(ConsoleColor.White);
        backgroundColorStack.Push(ConsoleColor.Black);

        Span<char> txt = text.ToCharArray();
        var (beforeFore, beforeBack) = GetColors();
        PeekColor();
        var index = 0;

        while (RegMatch.IsMatch(txt, index))
        {
            var match = RegMatch.Match(text, index);
            var matchValue = match.Value;
            var nIndex = text.IndexOf(matchValue, index, StringComparison.Ordinal);
            var isBack = match.Groups[1].Value == "!";

            Console.Write(txt[index..nIndex].ToString());

            if (match.Groups[3].Value == "@") continue;
            var rawValue = match.Groups[2].Value;
            ConsoleColor? color = rawValue == "r" ? null : Enum.Parse<ConsoleColor>(rawValue, true);

            switch (isBack)
            {
                case true when color is null && backgroundColorStack.Count > 1:
                    backgroundColorStack.Pop();
                    break;
                case true when color is not null:
                    backgroundColorStack.Push(color.Value);
                    break;
                default:
                    if (color is null && foregroundColorStack.Count > 1) foregroundColorStack.Pop();
                    else if (color is not null) foregroundColorStack.Push(color.Value);
                    break;
            }

            PeekColor();
            index = nIndex + matchValue.Length;
        }

        if (index < txt.Length) Console.Write(txt[index..].ToString());

        SetColors(beforeFore, beforeBack);
        return;

        void PeekColor() { SetColors(foregroundColorStack.Peek(), backgroundColorStack.Peek()); }
    }

    private static void WriteAscii(string text)
    {
        AsciiReset();
        Stack<Color> foregroundColorStack = new();
        Stack<Color> backgroundColorStack = new();
        var lastForeground = Color.White;
        var lastBackground = Color.Black;

        Span<char> txt = text.ToCharArray();
        var index = 0;

        while (RegMatch.IsMatch(txt, index))
        {
            var match = RegMatch.Match(text, index);
            var matchValue = match.Value;
            var nIndex = text.IndexOf(matchValue, index, StringComparison.Ordinal);
            var isBack = match.Groups[1].Value == "!";

            Console.Write(txt[index..nIndex].ToString());

            var rawValue = match.Groups[2].Value.ToLower();
            if (match.Groups[3].Value == "@")
            {
                AsciiSet(match.Groups[4].Value.ToLower() switch
                {
                    "r" => ResetNormal,
                    "b" => BoldOrIncreaseIntensity,
                    "/b" => BoldOffOrDoubleUnderline,
                    "i" => Italic,
                    "/i" => NotItalic,
                    "u" => Underline,
                    "uu" => BoldOffOrDoubleUnderline,
                    "/u" or "/uu" => UnderlineOff,
                    "blink" => RapidBlink,
                    "/blink" => BlinkOff,
                    "o" => Overlined,
                    "/o" => NotOverlined,
                    "s" => CrossOut,
                    "/s" => NotCrossOut,
                    "swap" => SwapFgAndBgColor,
                    "/swap" => SwapFgAndBgColorOff,
                    _ => NA
                });
            }
            else
            {
                switch (isBack)
                {
                    case true when rawValue == "r" && backgroundColorStack.Count > 1:
                        backgroundColorStack.Pop();
                        break;
                    case true when rawValue != "r":
                        backgroundColorStack.Push(rawValue.ToColor());
                        break;
                    default:
                        if (rawValue == "r" && foregroundColorStack.Count > 1) foregroundColorStack.Pop();
                        else if (rawValue != "r") foregroundColorStack.Push(rawValue.ToColor());
                        break;
                }
            }

            PeekColor();
            index = nIndex + matchValue.Length;
        }

        if (index < txt.Length) Console.Write(txt[index..].ToString());

        AsciiReset();
        return;

        void PeekColor()
        {
            Color nextFg;
            Color nextBg;
            if (CanSetColor(nextFg = NextColor(foregroundColorStack, Color.White), lastForeground))
            {
                AsciiSetFgColorRgb(nextFg);
                lastForeground = nextFg;
            }

            if (CanSetColor(nextBg = NextColor(backgroundColorStack, Color.Black), lastBackground))
            {
                AsciiSetBgColorRgb(nextBg);
                lastBackground = nextBg;
            }
        }

        Color NextColor(Stack<Color> colorStack, Color defColor)
        {
            return colorStack.Count == 0 ? defColor : colorStack.Peek();
        }

        bool CanSetColor(Color nextColor, Color lastColor) { return nextColor != lastColor; }
    }

    public static void SetColors(ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (foreground is not null) Console.ForegroundColor = foreground.Value;
        if (background is not null) Console.BackgroundColor = background.Value;
    }

    public static void WaitForSpaceInput()
    {
        WriteLine("\nPress the Spacebar to continue . . . ");
        while (GetKey() != Spacebar) ;
    }

    public static void WaitForAnyInput()
    {
        WriteLine("\nPress any key to continue . . . ");
        GetKey();
    }

    public static bool YesNoChoice()
    {
        var selected = true;
        var pos = GetCursor();
        while (true)
        {
            SetCursor(pos);
            Write(selected ? "   [!green]Yes[!r]     [#red]no" : "   [#green]Yes[#r]     [!red]no");

            switch (GetKey())
            {
                case A or D or LeftArrow or RightArrow:
                    selected = !selected;
                    break;
                case Enter or Spacebar:
                    MoveCursorBy(0, 1, true);
                    return selected;
            }
        }
    }

    public static double ProgressBar(double value, double max, int segmentCount, Func<double, ConsoleColor> getColor)
    {
        var delta = value / max;
        var segmentsUsed = (int)(segmentCount * delta);
        if (segmentsUsed == 0 && delta > 0) segmentsUsed = 1;

        Write($"[[!{getColor(delta)}]{' '.Repeat(segmentsUsed)}[!r]{' '.Repeat(segmentCount - segmentsUsed)}]");
        return delta;
    }

    public static double ProgressBar(double value, double max, int segmentCount, ConsoleColor color)
    {
        return ProgressBar(value, max, segmentCount, _ => color);
    }

    public static void MoveCursorBy(int x, int y = 0, bool setX = false)
    {
        var (pX, pY) = GetCursor();
        SetCursor(setX ? x : pX + x, pY + y);
    }

    public static void Write(char c) { Console.Write(c); }
    public static void Write(object s) { Write(s.ToString()); }
    public static void WriteLine(string s) { Write($"{s}\n"); }
    public static void WriteLine(object s) { Write($"{s}\n"); }
    public static void WriteLine(char c) { Console.WriteLine(c); }
    public static ConsoleKey GetKey() { return Console.ReadKey(true).Key; }
    public static string CleanColors(string text) { return RegMatch.Replace(text, string.Empty); }

    public static (ConsoleColor, ConsoleColor) GetColors()
    {
        return (Console.ForegroundColor, Console.BackgroundColor);
    }

    public static Pos GetCursor() { return Console.GetCursorPosition(); }
    public static void SetCursor(int x = 0, int y = 0) { Console.SetCursorPosition(x, y); }
    public static void SetCursor(Pos pos) { Console.SetCursorPosition(pos.X, pos.Y); }
    public static void CursorVis(bool condition = true) { Console.CursorVisible = condition; }
    public static void Clr() { Console.Clear(); }
    public static void AsciiReset() { AsciiSet(ResetNormal); }

    public static void AsciiSet(AsciiEffects id)
    {
        if (id == NA) return;
        Console.Write($"\e[{(int)id}m");
    }

    public static void AsciiSetFgColorRgb(string color) { Console.Write($"\e[38;2;{color}m"); }

    public static void AsciiSetBgColorRgb(string color) { Console.Write($"\e[48;2;{color}m"); }
    public static void AsciiSetFgColorRgb(Color color) { Console.Write($"\e[38;2;{color.R};{color.G};{color.B}m"); }
    public static void AsciiSetBgColorRgb(Color color) { Console.Write($"\e[48;2;{color.R};{color.G};{color.B}m"); }
}

public enum AsciiEffects
{
    NA = -1,
    ResetNormal = 0,
    BoldOrIncreaseIntensity = 1,
    FaintOrDecreaseIntensity = 2,
    Italic = 3,
    Underline = 4,
    SlowBlink = 5,
    RapidBlink = 6,
    SwapFgAndBgColor = 7,
    Conceal = 8,
    CrossOut = 9,
    PrimaryFont = 10,
    AltFont1 = 11,
    AltFont2 = 12,
    AltFont3 = 13,
    AltFont4 = 14,
    AltFont5 = 15,
    AltFont6 = 16,
    AltFont7 = 17,
    AltFont8 = 18,
    AltFont9 = 19,
    BoldOffOrDoubleUnderline = 21,
    NormalColorOrIntensity = 22,
    NotItalic = 23,
    UnderlineOff = 24,
    BlinkOff = 25,
    SwapFgAndBgColorOff = 27,
    ConcealOff = 28,
    NotCrossOut = 29,
    FgBlack = 30,
    FgRed = 31,
    FgGreen = 32,
    FgYellow = 33,
    FgBlue = 34,
    FgMagenta = 35,
    FgCyan = 36,
    FgWhite = 37,
    SetFgColor = 38,
    DefaultFgColor = 39,
    BgBlack = 40,
    BgRed = 41,
    BgGreen = 42,
    BgYellow = 43,
    BgBlue = 44,
    BgMagenta = 45,
    BgCyan = 46,
    BgWhite = 47,
    SetBgColor = 48,
    DefaultBgColor = 49,
    Overlined = 53,
    NotOverlined = 55,
    FgBrightBlack = 90,
    FgBrightRed = 91,
    FgBrightGreen = 92,
    FgBrightYellow = 93,
    FgBrightBlue = 94,
    FgBrightMagenta = 95,
    FgBrightCyan = 96,
    FgBrightWhite = 97,
    BgBrightBlack = 100,
    BgBrightRed = 101,
    BgBrightGreen = 102,
    BgBrightYellow = 103,
    BgBrightBlue = 104,
    BgBrightMagenta = 105,
    BgBrightCyan = 106,
    BgBrightWhite = 107,
}

public static class Helper
{
    public static string Repeat(this char character, int count)
    {
        return string.Join("", Enumerable.Repeat(character, count));
    }

    public static string Repeat(this string text, int count) { return string.Join("", Enumerable.Repeat(text, count)); }
}