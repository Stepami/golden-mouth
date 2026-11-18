using Reki.Domain.Player;

namespace Reki.Application.Player.Responses;

public sealed record PlayerResponse(
    PlayerId Id,
    Address Address,
    int Energy,
    IReadOnlyDictionary<SkillId, SkillResponse> Skills,
    PlayerSettingsResponse Settings,
    int BirthYear,
    DailyChallengeResponse DailyChallenge,
    bool HitOwnWorldMilestone = false,
    EnergyWalletResponse? EnergyWallet = null);
