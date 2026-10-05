namespace Reki.Domain.Player;

public sealed record SkillProgress(int Level, double Progress, double Experience)
{
    public SkillProgress AddExperience(double amount, IReadOnlyList<double> levelThresholds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (levelThresholds.Count != 10)
        {
            throw new ArgumentException("Exactly ten skill thresholds are required.", nameof(levelThresholds));
        }

        double currentExperience = Experience > 0 || Level == 1
            ? Experience
            : InferExperience(levelThresholds);
        double experience = Math.Min(levelThresholds[^1], currentExperience + amount);
        int levelIndex = 0;
        while (levelIndex + 1 < levelThresholds.Count && experience >= levelThresholds[levelIndex + 1])
        {
            levelIndex++;
        }

        int level = levelIndex + 1;
        double progress = level == levelThresholds.Count
            ? 1
            : (experience - levelThresholds[levelIndex]) /
                (levelThresholds[levelIndex + 1] - levelThresholds[levelIndex]);
        return new SkillProgress(level, progress, experience);
    }

    private double InferExperience(IReadOnlyList<double> levelThresholds)
    {
        int levelIndex = Math.Clamp(Level - 1, 0, levelThresholds.Count - 1);
        if (levelIndex == levelThresholds.Count - 1)
        {
            return levelThresholds[^1];
        }

        double levelStart = levelThresholds[levelIndex];
        double levelEnd = levelThresholds[levelIndex + 1];
        return levelStart + ((levelEnd - levelStart) * Math.Clamp(Progress, 0, 1));
    }
}

