namespace Reki.Application.Balance;

public sealed record SkillBalance(
    IReadOnlyList<double> LevelThresholds,
    double MainExperience,
    double SideExperience,
    double OwnWordsMultiplier);
