namespace Reki.Application.Common;

public sealed record WorldBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom);
