namespace Reki.Application.Common.Balance;

public sealed record WorldBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom);
