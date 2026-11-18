using Reki.Domain.Player.Challenges;
using Reki.Domain.Worlds;

namespace Reki.Application.Player.Responses;

public sealed record DailyChallengeResponse(
    DateOnly Day,
    IReadOnlyDictionary<DailyTaskType, bool> Completion,
    IReadOnlyDictionary<WorldId, IReadOnlyDictionary<DailyTaskType, DailyTaskValuesResponse>> Values);
