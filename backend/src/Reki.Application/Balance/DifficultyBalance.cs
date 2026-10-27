namespace Reki.Application.Balance;

public sealed record DifficultyBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom);
