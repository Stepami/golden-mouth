namespace Reki.Application.Common;

public sealed record SkillBalance(
    IReadOnlyList<double> LevelThresholds,
    double MainExperience,
    double SideExperience,
    int ExperiencePostLimit,
    double FirstPostsMultiplier,
    int FirstPostsCount,
    double OwnWordsMultiplier,
    double WantDoneMultiplier);
