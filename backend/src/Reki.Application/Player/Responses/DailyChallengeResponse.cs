namespace Reki.Application.Player.Responses;

public sealed record DailyChallengeResponse(
    DateOnly Day,
    IReadOnlyDictionary<string, bool> Completion,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, DailyTaskValuesResponse>> Values);
