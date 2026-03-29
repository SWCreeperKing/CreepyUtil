namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{
    private bool HasGoaledCached;
    private int GoalType = 0;

    public int GetGoalType() => GoalType;
    public TEnum GetGoalTypeAsEnum<TEnum>() where TEnum : struct, Enum => (TEnum)(object)GoalType;
    public bool IsGoalType<TEnum>(TEnum type) where TEnum : struct, Enum => GoalType == (int)(object)type;
    public void SetGoalType<TEnum>(TEnum type) where TEnum : struct, Enum => GoalType = (int)(object)type;
    public void SetGoalType(int type) => GoalType = type;
    public bool TryGoal<TEnum>(TEnum goalType) where TEnum : struct, Enum => TryGoal((int)(object)goalType);

    public bool TryGoal(int goalType = 0)
    {
        if (goalType != GoalType) return false;
        if (HasGoaledCached) return true;
        Session?.SetGoalAchieved();
        HasGoaledCached = true;
        return true;
    }
}