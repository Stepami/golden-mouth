using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

public sealed record DifficultyBalance(
    double GrowthCoefficient,
    double FlopChance,
    double ViralMultiplier,
    long HateFrom,
    double LossMultiplier = 0,
    double MaxSubscriberFraction = 0,
    double AngryAbuse = 1,
    double SadTone = 1,
    double AnxiousTone = 1,
    double AngryStep = 0,
    double SadStep = 0,
    double AnxiousStep = 0)
{
    public IReadOnlyDictionary<WorldDrama, DramaBalance> Drama { get; init; } = new Dictionary<WorldDrama, DramaBalance>();
}
