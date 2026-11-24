using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

public sealed record DifficultyBalance(
    double GrowthCoefficient,
    int ViralMultiplier,
    long HateFrom,
    double TopicMinCosine,
    IReadOnlyDictionary<string, double> Thresholds,
    IReadOnlyDictionary<string, double> ThresholdReductionPerStep,
    IReadOnlyDictionary<WorldDrama, DramaBalance> Drama,
    string EmotionTieBreak);
