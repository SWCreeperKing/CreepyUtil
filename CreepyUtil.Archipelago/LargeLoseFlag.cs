namespace CreepyUtil.Archipelago;

public class LargeLoseFlag<T>
{
    private string TrackingFlag;
    private List<T> FlagMap = [];

    public string MaxFlag => TrackingFlag.Replace("0", "1");
    public bool IsMaxFlag => TrackingFlag == MaxFlag;

    public LargeLoseFlag(string flag = "") => SetFlag(flag);

    public void AddFlag(T flag)
    {
        FlagMap.Add(flag);
        FillMissingSize();
    }

    public void AddFlags(params T[] flags)
    {
        FlagMap.AddRange(flags);
        FillMissingSize();
    }

    public void SetFlag(string flag = "")
    {
        if (flag is not "" && flag.Any(s => s is not ('1' or '0')))
            throw new ArgumentException("Input flag is not in correct format");
        TrackingFlag = flag;
    }

    private void FillMissingSize()
    {
        if (FlagMap.Count <= TrackingFlag.Length) return;
        TrackingFlag += string.Join("", Enumerable.Repeat("0", FlagMap.Count - TrackingFlag.Length));
    }

    private LargeLoseFlag<T> ReplaceAt(bool set, T flag)
    {
        var index = FlagMap.IndexOf(flag);
        TrackingFlag = TrackingFlag.Remove(index, 1).Insert(index, set ? "1" : "0");
        return this;
    }

    public bool HasFlag(T flag) => TrackingFlag[FlagMap.IndexOf(flag)] is '1';
    public static LargeLoseFlag<T> operator +(LargeLoseFlag<T> flagA, T flagB) => flagA.ReplaceAt(true, flagB);
    public static LargeLoseFlag<T> operator -(LargeLoseFlag<T> flagA, T flagB) => flagA.ReplaceAt(false, flagB);
    public static implicit operator string(LargeLoseFlag<T> flag) => flag.TrackingFlag;
    public bool this[T flag] => HasFlag(flag);
}