using Reki.Domain.Social;

namespace Reki.Domain.Player;

public sealed record StarRelationProgress(
    StarId StarId,
    int Value,
    int GainedInPeriod,
    DateTimeOffset PeriodStartedAtUtc);
