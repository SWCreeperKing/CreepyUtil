using System.Text.RegularExpressions;
using CreepyUtil;
using static System.ConsoleKey;

namespace RedefinedRpg;

public static class ClrCnsl
{
    public const char Block = '\u2588';
    public const int ListLeng = 11;
    public static readonly Regex RegMatch = new(@"\[(#|!)(\w+?)\]", RegexOptions.Compiled);
    private static readonly string[] SourceArray = ["^", "v"];

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
            var left = (int) Math.Floor(halfRemainder) + s.Length;
            var right = (int) Math.Ceiling(halfRemainder);
            return s.PadLeft(left).PadRight(left + right);
        }
    }

    private static int DisplayUpdateListView(bool isMore, string[] formattedOptions, string[] moreLeng, int selected,
        string[] options)
    {
        if (isMore)
        {
            WriteLine($"[#yellow]{moreLeng[0]}");
        }

        for (var i = 0; i < Math.Min(formattedOptions.Length, ListLeng); i++)
        {
            var rI = i + Math.Max(0,
                Math.Min(formattedOptions.Length - ListLeng, selected + 1 - (int) Math.Ceiling(ListLeng / 2f)));
            var isS = selected == rI;
            WriteLine(
                $"{(isS ? "[#green] >[!darkgray][#cyan]" : "  [#blue]")}{formattedOptions[rI]}{(isS ? "[#green][!black]< " : "  ")}");
        }

        if (isMore)
        {
            WriteLine($"[#yellow]{moreLeng[1]}");
        }

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

        if (index < txt.Length)
        {
            Console.Write(txt[index..].ToString());
        }

        SetColors(beforeFore, beforeBack);
        return;

        void PeekColor() => SetColors(foregroundColorStack.Peek(), backgroundColorStack.Peek());
    }

    public static void SetColors(ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (foreground is not null)
        {
            Console.ForegroundColor = foreground.Value;
        }

        if (background is not null)
        {
            Console.BackgroundColor = background.Value;
        }
    }

    public static void WaitForSpaceInput()
    {
        WriteLine("\nPress the Spacebar to continue . . . ");
        while (GetKey() != Spacebar)
        {
        }
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
        var segmentsUsed = (int) (segmentCount * delta);
        if (segmentsUsed == 0 && delta > 0)
        {
            segmentsUsed = 1;
        }

        Write($"[#{getColor(delta)}][{Block.Repeat(segmentsUsed)}{' '.Repeat(segmentCount - segmentsUsed)}][#r]");
        return delta;
    }

    public static double ProgressBar(double value, double max, int segmentCount, ConsoleColor color)
        => ProgressBar(value, max, segmentCount, _ => color);

    public static void MoveCursorBy(int x, int y = 0, bool setX = false)
    {
        var (pX, pY) = GetCursor();
        SetCursor(setX ? x : pX + x, pY + y);
    }

    public static void Write(char c) => Console.Write(c);
    public static void Write(object s) => Write(s.ToString());
    public static void WriteLine(string s) => Write($"{s}\n");
    public static void WriteLine(object s) => Write($"{s}\n");
    public static void WriteLine(char c) => Console.WriteLine(c);
    public static ConsoleKey GetKey() => Console.ReadKey(true).Key;
    public static string CleanColors(string text) => RegMatch.Replace(text, string.Empty);
    public static (ConsoleColor, ConsoleColor) GetColors() => (Console.ForegroundColor, Console.BackgroundColor);
    public static Pos GetCursor() => Console.GetCursorPosition();
    public static void SetCursor(int x = 0, int y = 0) => Console.SetCursorPosition(x, y);
    public static void SetCursor(Pos pos) => Console.SetCursorPosition(pos.X, pos.Y);
    public static void CursorVis(bool condition = true) => Console.CursorVisible = condition;
    public static void Clr() => Console.Clear();
}

public static class Helper
{
    public static string Repeat(this char character, int count) => string.Join("", Enumerable.Repeat(character, count));
    public static string Repeat(this string text, int count) => string.Join("", Enumerable.Repeat(text, count));
}