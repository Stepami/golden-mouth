namespace Reki.Application.Common;

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
    IReadOnlyDictionary<string, WorldBalance> Worlds,
    int PostEnergyCost = 1,
    int OwnWorldSubscribers = 500);
