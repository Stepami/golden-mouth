using Reki.Domain.Worlds;

namespace Reki.Application.Balance;

public sealed record GameBalance(
    int FreePostsPerDay,
    int PlusPostsPerDay,
    TimeOnly PostsResetTimeMoscow,
    int InviteBonusBoth,
    int InviteBonusMaxPerDay,
    int PlusInterestBonus,
    int ChallengeWinBonusPerDay,
    int ChallengeWinBonusDays,
    GrowthBalance Growth,
    ViralBalance Viral,
    FlopBalance Flop,
    SkillBalance Skills,
    RelationBalance Relations,
    ChallengeBalance Challenge,
    ThreadBalance Thread,
    IReadOnlyDictionary<WorldDifficulty, DifficultyBalance> Difficulties,
    int PostEnergyCost = 1,
    int OwnWorldSubscribers = 500,
    double StarFeedOpenProbability = 0,
    int EnergyCap = 10,
    double RepostGrowthMultiplier = 0.25);
