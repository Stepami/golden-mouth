using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

public sealed record GameBalance(
    int FreePostsPerDay,
    int PlusPostsPerDay,
    int InviteBonusBoth,
    int PlusInterestBonus,
    GrowthBalance Growth,
    ViralBalance Viral,
    SkillBalance Skills,
    ThreadBalance Thread,
    IReadOnlyDictionary<WorldDifficulty, DifficultyBalance> Difficulties,
    int PostEnergyCost = 1,
    int OwnWorldSubscribers = 500,
    double StarFeedOpenProbability = 0,
    int EnergyCap = 10);
