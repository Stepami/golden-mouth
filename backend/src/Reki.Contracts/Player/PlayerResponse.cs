namespace Reki.Contracts.Player;

public sealed record PlayerResponse(
    string Id,
    Address Address,
    int PostsLeft,
    int PostsCap,
    int FirstPostInviteBonus,
    StreakResponse Streak,
    IReadOnlyDictionary<SkillId, SkillResponse> Skills,
    PlayerSettingsResponse Settings,
    IReadOnlyDictionary<string, string> Experiments,
    bool HitOwnWorldMilestone = false,
    string AgeBand = "");