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

        var experience = Math.Min(levelThresholds[^1], Experience + amount);
        var levelIndex = 0;
        while (levelIndex + 1 < levelThresholds.Count && experience >= levelThresholds[levelIndex + 1])
        {
            levelIndex++;
        }

        var level = levelIndex + 1;
        var progress = level == levelThresholds.Count
            ? 1
            : (experience - levelThresholds[levelIndex]) /
                (levelThresholds[levelIndex + 1] - levelThresholds[levelIndex]);
        return new SkillProgress(level, progress, experience);
    }
}
