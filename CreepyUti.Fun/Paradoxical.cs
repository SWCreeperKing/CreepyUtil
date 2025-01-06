namespace CreepyUti.Fun;

public readonly struct Paradoxical(double percent)
{
    private static readonly Random Random = new();
    public readonly double Percent = percent;
    public bool Value() { return Random.NextDouble() < Percent / 100d; }
    public static implicit operator bool(Paradoxical paradoxical) { return paradoxical.Value(); }
    public override string ToString() { return $"{Value()}?"; }
}