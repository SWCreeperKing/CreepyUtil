namespace CreepyUtil.Archipelago.ApClient;

public partial class ApClient
{    
    public static double DeltaTime { get; private set; } = 0;
    
    private static double LastTimeStep = 0;
    
    public static void ResetDeltaTime()
    {
        LastTimeStep = 0;
    }
    
    public static double UpdateDeltaTime()
    {
        var now = DateTime.UtcNow.TimeOfDay.TotalMilliseconds/1000d;
        if (LastTimeStep == 0)
        {
            LastTimeStep = now;
            return 0;
        }

        var delta = now - LastTimeStep;
        LastTimeStep = now;
        return DeltaTime = delta;
    }
}