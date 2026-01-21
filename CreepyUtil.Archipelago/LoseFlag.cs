namespace CreepyUtil.Archipelago;

public class LoseFlag<T>
{
    private ulong TrackingFlag = 0;
    private T[] FlagMap = new T[64];
    private byte FlagsFilled = 1;

    public LoseFlag(T flagZero, ulong flag = 0)
    {
        FlagMap[0] = flagZero;
        TrackingFlag = flag;
    }
    
    public void AddFlag(T flag)
    {
        if (FlagsFilled >= FlagMap.Length) throw new ArgumentOutOfRangeException(nameof(flag), "No more spots to add flags to");

        FlagMap[FlagsFilled++] = flag;
    }

    public void AddFlags(params T[] flags)
    {
        foreach (var flag in flags)
        {
            AddFlag(flag);
        }
    }

    public bool HasFlag(T flag)
    {
        var index = Array.IndexOf(FlagMap, flag);
        if (index == -1) throw new ArgumentOutOfRangeException(nameof(flag), "Given flag does not exist in map");
        
        return (TrackingFlag & 1ul << index) != 0;
    }
    
    public bool HasFlag(ulong flag) => (TrackingFlag & flag) != 0;

    public static implicit operator ulong(LoseFlag<T> flag) => flag.TrackingFlag;

    public static LoseFlag<T> operator +(LoseFlag<T> flagA, ulong flagB)
    {
        flagA.TrackingFlag |= flagB;
        return flagA;
    }

    public static LoseFlag<T> operator -(LoseFlag<T> flagA, ulong flagB)
    {
        if (!flagA[flagB]) return flagA;
        flagA.TrackingFlag &= ~flagB;
        return flagA;
    }
    
    public bool this[T flag] => HasFlag(flag);
    public bool this[ulong flag] => HasFlag(flag);
}