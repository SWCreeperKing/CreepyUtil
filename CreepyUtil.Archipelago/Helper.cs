using System.Text;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;

namespace CreepyUtil.Archipelago;

public static class Helper
{
    public static bool RunWithTimeout(this Task task, TimeSpan timeout, Action<Exception>? onError)
    {
        try
        {
            task.Start();
            return Task.WaitAny([task], timeout) != -1;
        }
        catch (Exception e)
        {
            onError?.Invoke(e);
        }
        return false;
    }

    public static int SortNumber(this HintStatus status)
    {
        return status switch
        {
            HintStatus.Found => 0,
            HintStatus.NoPriority => 2,
            HintStatus.Avoid => 4,
            HintStatus.Priority => 1,
            _ => 3,
        };
    }

    public static int SortNumber(this ItemFlags item)
    {
        if (item.HasFlag(ItemFlags.Advancement)) return 0;
        if (item.HasFlag(ItemFlags.Trap)) return 10;
        return item.HasFlag(ItemFlags.NeverExclude) ? 1 : 2;
    }

    public static string GetAsTime(this double time, bool staticSecEnding = true)
    {
        var sec = time % 60;
        time = Math.Floor(time / 60f);
        var min = time % 60;
        time = Math.Floor(time / 60f);
        var hour = time % 24;
        var days = Math.Floor(time / 24f);

        StringBuilder sb = new();
        if (days > 0) sb.Append(days).Append("d ");
        if (hour > 0) sb.Append(hour).Append("hr ");
        if (min > 0) sb.Append(min).Append("m ");
        switch (sec)
        {
            case > 0 when staticSecEnding:
                sb.Append($"{sec:#0.00}").Append("s ");
                break;
            case > 0:
                sb.Append($"{sec:#0.##}").Append("s ");
                break;
        }

        if (sb.Length == 0) sb.Append("0s ");
        return sb.ToString().TrimEnd();
    }

    public static HashSet<Hint> OrderHints(this IEnumerable<Hint> hints, int playerCount, IEnumerable<int> PlayerSlots)
    {
        return hints
              .OrderBy(hint => hint.Status.SortNumber())
              .ThenBy(hint => hint.ItemFlags.SortNumber())
              .ThenBy(hint
                   => PlayerSlots.Contains(hint.ReceivingPlayer)
                       ? playerCount + 1
                       : hint.ReceivingPlayer)
              .ThenBy(hint
                   => PlayerSlots.Contains(hint.FindingPlayer)
                       ? playerCount + 1
                       : hint.FindingPlayer)
              .ThenBy(hint => hint.LocationId)
              .ToHashSet();
    }

    public static T? SafeTo<T>(this DataStorageElement? element, T? def = default)
    {
        try
        {
            return element!.To<T>() ?? def;
        }
        catch
        {
            return def;
        }
    }

    public static string[] SplitAndTrim(this string text, char delimiter) => text.Split(delimiter).Select(s => s.Trim()).Where(s => s is not "").ToArray();
}