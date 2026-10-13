using Reki.Domain.Social;

namespace Reki.Domain.Worlds;

public sealed record WorldStar(
    StarId Id,
    string Name,
    string Role,
    StarTrait Trait,
    string About,
    string Voice,
    string AvatarSeed,
    int Relation,
    int RelationGainedInPeriod,
    DateTimeOffset RelationPeriodStartedAtUtc,
    string AppearanceProfile = "neutral",
    string AgeBand = "");
