namespace Reki.Application.Balance;

public sealed record BonusBalance(
    int OnboardingCrystals,
    int ViralCardMultiplier,
    int FlopGuardViews,
    int BoosterHours,
    int BoosterExtensionDays,
    int SkillLevelCrystals,
    int PostInterval,
    int PostIntervalCrystals,
    int ViralPostCrystals,
    int ChallengeCrystals,
    IReadOnlyList<int> StreakDays,
    int StreakInterval,
    int StreakCrystals);
