using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

public sealed record DifficultyBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom,
    double LossMultiplier,
    double MaxSubscriberFraction,
    IReadOnlyDictionary<string, double> Thresholds,
    IReadOnlyDictionary<string, double> ThresholdReductionPerStep,
    IReadOnlyDictionary<WorldDrama, DramaBalance> Drama,
    IReadOnlyList<string> ReasonPriority,
    string EmotionTieBreak);
