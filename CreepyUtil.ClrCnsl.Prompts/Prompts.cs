using static CreepyUtil.ClrCnsl.ClrCnsl;

namespace CreepyUtil.ClrCnsl.Prompts;

public static class Prompts
{
    public static T ConfirmAction<T>(Func<T> action, Func<T, string> confirm)
    {
        while (true)
        {
            var name = action();
            WriteLine(confirm(name));
            if (YesNoChoice()) return name;
            Clr();
        }
    }

    public static string InputPrompt(string prompt)
    {
        CursorVis();
        WriteLine(prompt);
        var input = Console.ReadLine()!;
        CursorVis(false);
        Clr();
        return input;
    }

    public static bool YesNoPrompt(string prompt)
    {
        WriteLine(prompt);
        return YesNoChoice();
    }

    public static string YesNoConfirmInput(string prompt, Func<string, string> confirm)
    {
        return ConfirmAction(() => InputPrompt(prompt), confirm);
    }

    public static T YesNoPromptTest<T>(string prompt, Func<string, string> confirm,
        Func<string, (T obj, bool success)> test,
        Func<string, string> failPrompt)
    {
        while (true)
        {
            var input = YesNoConfirmInput(prompt, confirm);
            var (obj, success) = test(input);
            if (success) return obj;

            Clr();
            WriteLine($"[#red]{failPrompt(input)}[#r]");
        }
    }

    public static int ListViewPrompt(string prompt, int selection = 0, params string[] options)
    {
        WriteLine(prompt);
        return ListView(selection, options);
    }

    public static T ListViewConfirmType<T>(string prompt, Func<T, string> confirm, Func<T, string> toString,
        int selection = 0, params T[] options)
    {
        var list = options.Select(toString).ToArray();
        return options[ConfirmAction(() => ListViewPrompt(prompt, selection, list), i => confirm(options[i]))];
    }
}