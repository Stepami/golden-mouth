namespace Reki.Application.Player.Responses;

public sealed record PlayerResponse(
    string Id,
    Address Address,
    int Energy,
    StreakResponse Streak,
    IReadOnlyDictionary<SkillIdType, SkillResponse> Skills,
    PlayerSettingsResponse Settings,
    IReadOnlyDictionary<string, string> Experiments,
    bool HitOwnWorldMilestone = false,
    string AgeBand = "");