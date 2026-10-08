namespace Reki.Application.Common;

public sealed record WorldBalance(double GrowthCoefficient, double FlopChance, double ViralMultiplier)
{
    public long HateFrom { get; init; } = long.MaxValue;
}
