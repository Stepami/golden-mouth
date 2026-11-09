using Reki.Domain.Player;

namespace Reki.Application.Player.Responses;

public sealed record PlayerResponse(
    PlayerId Id,
    Address Address,
    int Energy,
    StreakResponse Streak,
    IReadOnlyDictionary<SkillId, SkillResponse> Skills,
    PlayerSettingsResponse Settings,
    IReadOnlyDictionary<string, string> Experiments,
    bool HitOwnWorldMilestone = false,
    string AgeBand = "",
    bool OnboardingCompleted = false);
