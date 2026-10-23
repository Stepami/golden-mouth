namespace Reki.Application.Balance;

public sealed record WorldBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom);
