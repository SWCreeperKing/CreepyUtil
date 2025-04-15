using System.Text;
using System.Text.RegularExpressions;
using static System.ConsoleKey;
using static CreepyUtil.ClrCnsl.AsciiEffects;
using static CreepyUtil.ClrCnsl.ColumnSetting;

namespace CreepyUtil.ClrCnsl;

public static class ClrCnsl
{
    // public const char Block = '\u2588';
    private static readonly Dictionary<string, string> CleanColorCache = [];

    public const int ListLeng = 11;

    public static readonly Regex RegMatch = new(@"\[(?:(#|!)([a-zA-Z0-9]+?|\d{1,3},\d{1,3},\d{1,3})|(@|~)(\w+?))\]",
        RegexOptions.Compiled);

    private static readonly string[] SourceArray = ["^", "v"];

    public static bool UseAscii;

    private static List<Task> WriteUpdates = [];
    private static bool WhileWriteUpdates = true;

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
                $"{(isS ? "[#green] >[!darkgray][#cyan]" : "  [#blue]")}{formattedOptions[rI]}{(isS ? "[@r][#green]< " : "  ")}");
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


            var stack = isBack ? backgroundColorStack : foregroundColorStack;
            if (rawValue == "r")
            {
                if (stack.Count > 0)
                {
                    stack.Pop();
                }
            }
            else
            {
                stack.Push(color!.Value);
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

        PeekColor();
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
                    "i" => Italic,
                    "u" => Underline,
                    "uu" => BoldOffOrDoubleUnderline,
                    "blink" => RapidBlink,
                    "o" => Overlined,
                    "s" => CrossOut,
                    "swap" => SwapFgAndBgColor,
                    _ => NA
                });
            }
            else if (match.Groups[3].Value == "~")
            {
                AsciiSet(match.Groups[4].Value.ToLower() switch
                {
                    "b" => BoldOffOrDoubleUnderline,
                    "i" => NotItalic,
                    "u" or "uu" => UnderlineOff,
                    "blink" => BlinkOff,
                    "o" => NotOverlined,
                    "s" => NotCrossOut,
                    "swap" => SwapFgAndBgColorOff,
                    _ => NA
                });
            }
            else
            {
                var stack = isBack ? backgroundColorStack : foregroundColorStack;
                if (rawValue == "r")
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                    }
                }
                else
                {
                    stack.Push(rawValue.ToColor());
                }

                PeekColor();
            }

            index = nIndex + matchValue.Length;
        }

        if (index < txt.Length)
        {
            Console.Write(txt[index..].ToString());
        }

        Console.Write("\e[0m");
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

    public static void WriteBack(object text)
    {
        var pos = GetCursor();
        Write(text);
        SetCursor(pos);
    }

    public static void WriteUpdate(Func<object> action, bool newLine = true)
    {
        WriteUpdates.Add(Task.Run(async () =>
        {
            var pos = GetCursor();
            while (WhileWriteUpdates)
            {
                SetCursor(pos);
                if (newLine)
                {
                    WriteLine(action());
                }
                else
                {
                    Write(action());
                }
            }
        }));
    }

    public static void EndWriteUpdates()
    {
        WhileWriteUpdates = false;
        foreach (var task in WriteUpdates)
        {
            task.Wait();
            Task.Delay(1).GetAwaiter().GetResult();
            task.Dispose();
        }

        WriteUpdates.Clear();
        WhileWriteUpdates = true;
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

    public static string CleanColors(string? text)
    {
        text ??= "";
        if (CleanColorCache.TryGetValue(text, out var clean)) return clean;
        return CleanColorCache[text] = RegMatch.Replace(text, string.Empty);
    }

    public static (ConsoleColor, ConsoleColor) GetColors()
    {
        return (Console.ForegroundColor, Console.BackgroundColor);
    }

    public static Pos.Pos GetCursor() { return Console.GetCursorPosition(); }
    public static void SetCursor(int x = 0, int y = 0) { Console.SetCursorPosition(x, y); }
    public static void SetCursor(Pos.Pos pos) { Console.SetCursorPosition(pos.X, pos.Y); }
    public static void CursorVis(bool condition = true) { Console.CursorVisible = condition; }
    public static void Clr() { Console.Clear(); }
    public static void AsciiReset() { AsciiSet(ResetNormal); }

    public static void Table(string title, bool drawBetween, ColumnSetting[] settings, params List<string[]> table)
    {
        var columns = settings.Length;
        if (columns == 0) throw new ArgumentException("Table must have more than 0 columns");
        if (table.Any(l => l.Length != columns))
        {
            throw new ArgumentException(
                $"Table column amounts are inconsistent ({columns}) != ({string.Join(", ", table.GroupBy(l => l.Length).Select(g => g.Key))})");
        }

        var rowLengths = table.Aggregate(settings.Select(cs => CleanColors(cs.Name).Length).ToArray(), (a, b) =>
        {
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = Math.Max(a[i], CleanColors(b[i]).Length);
            }

            return a;
        });

        table.Insert(0, settings.Select(cs => cs.Name).ToArray());

        StringBuilder sb = new();
        sb.Append('+');
        for (var i = 0;
             i < columns;
             i++)
        {
            sb.Append(Repeat('-', rowLengths[i]));
            if (i != columns - 1 && settings[i].SeparatorChar != ' ')
            {
                sb.Append('+');
            }
            else if (i != columns - 1)
            {
                sb.Append('-');
            }
        }

        sb.Append('+');
        var row = sb.ToString();

        // +---- title ----+
        Write("+");
        WriteCenterPadding($"|{title}|", row.Length - 2, '-');

        WriteLine("+");
        for (var j = 0; j < table.Count; j++)
        {
            var background = j % 2 == 0 ? "[!black]" : "[!1E1E1E]";
            var line = table[j];
            Write($"{background}|");
            for (var i = 0; i < columns; i++)
            {
                switch (settings[i].Alignment)
                {
                    case Align.Left:
                        WriteLeftPadding($"{line[i]}", rowLengths[i], prefix:background);
                        break;
                    case Align.Center:
                        WriteCenterPadding($"{line[i]}", rowLengths[i], prefix:background);
                        break;
                    case Align.Right:
                        WriteRightPadding($"{line[i]}", rowLengths[i], prefix:background);
                        break;
                }

                Write($"{background}{(i == columns - 1 ? "|" : settings[i].SeparatorChar)}");
            }

            if (j == 0 || j == table.Count - 1 || drawBetween)
            {
                WriteLine($"\n{row}");
            }
            else
            {
                WriteLine("");
            }
        }
        // + [   ] | [   ] |
        // +-------+-------+
    }

    public static void WriteRightPadding(string text, int length, char fill = ' ', string prefix = "")
    {
        Write($"{prefix}{Repeat(fill, length - CleanColors(text).Length)}{text}");
    }

    public static void WriteLeftPadding(string text, int length, char fill = ' ', string prefix = "")
    {
        Write($"{prefix}{text}{Repeat(fill, length - CleanColors(text).Length)}");
    }

    public static void WriteCenterPadding(string text, int length, char fill = ' ', string prefix = "")
    {
        var leftOver = length - CleanColors(text).Length;
        Write($"{prefix}{Repeat(fill, (int)Math.Floor(leftOver / 2f))}{text}{Repeat(fill, (int)Math.Ceiling(leftOver / 2f))}");
    }

    public static string Space(int length) { return Repeat(' ', length); }
    public static string Repeat(char c, int length) { return string.Join("", Enumerable.Repeat(c, length)); }
    public static string Repeat(string s, int length) { return string.Join("", Enumerable.Repeat(s, length)); }

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

public readonly struct ColumnSetting(string name, Align align, char sepChar = '|')
{
    public enum Align
    {
        Left,
        Center,
        Right
    }

    public readonly string Name = name;
    public readonly Align Alignment = align;
    public readonly char SeparatorChar = sepChar;
}

public static class Helper
{
    public static string Repeat(this char character, int count)
    {
        return string.Join("", Enumerable.Repeat(character, count));
    }

    public static string Repeat(this string text, int count) { return string.Join("", Enumerable.Repeat(text, count)); }
}