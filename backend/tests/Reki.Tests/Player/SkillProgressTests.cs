using Reki.Domain.Player;

namespace Reki.Tests.Player;

public sealed class SkillProgressTests
{
    private static readonly double[] Thresholds = [0, 8, 20, 36, 56, 80, 110, 150, 200, 270];

    [Test]
    public async Task AddExperience_AtEveryLevelThreshold_AdvancesToExpectedLevelAsync()
    {
        // Arrange
        var initial = new SkillProgress(1, 0, 0);

        // Act
        SkillProgress[] results = Thresholds
            .Select(threshold => initial.AddExperience(threshold, Thresholds))
            .ToArray();

        // Assert
        for (int index = 0; index < results.Length; index++)
        {
            await Assert.That(results[index].Level).IsEqualTo(index + 1);
            await Assert.That(results[index].Experience).IsEqualTo(Thresholds[index]);
            await Assert.That(results[index].Progress).IsEqualTo(index == 9 ? 1 : 0);
        }
    }

    [Test]
    public async Task AddExperience_BetweenThresholds_ReportsProgressToNextLevelAsync()
    {
        // Arrange
        var initial = new SkillProgress(1, 0, 0);

        // Act
        SkillProgress result = initial.AddExperience(14, Thresholds);

        // Assert
        await Assert.That(result.Level).IsEqualTo(2);
        await Assert.That(result.Experience).IsEqualTo(14);
        await Assert.That(result.Progress).IsEqualTo(0.5);
    }

    [Test]
    public async Task AddExperience_LegacyProgressWithoutExperience_InfersCurrentExperienceAsync()
    {
        // Arrange
        var legacy = new SkillProgress(2, 0.5, 0);

        // Act
        SkillProgress result = legacy.AddExperience(6, Thresholds);

        // Assert
        await Assert.That(result.Experience).IsEqualTo(20);
        await Assert.That(result.Level).IsEqualTo(3);
        await Assert.That(result.Progress).IsEqualTo(0);
    }

    [Test]
    public async Task AddExperience_AboveMaximumThreshold_CapsAtMaximumAsync()
    {
        // Arrange
        var initial = new SkillProgress(9, 0.9, 263);

        // Act
        SkillProgress result = initial.AddExperience(20, Thresholds);

        // Assert
        await Assert.That(result.Level).IsEqualTo(10);
        await Assert.That(result.Experience).IsEqualTo(270);
        await Assert.That(result.Progress).IsEqualTo(1);
    }

    [Test]
    public async Task AddExperience_NegativeAmount_ThrowsArgumentOutOfRangeExceptionAsync()
    {
        // Arrange
        var initial = new SkillProgress(1, 0, 0);
        ArgumentOutOfRangeException? exception = null;

        // Act
        try
        {
            initial.AddExperience(-0.1, Thresholds);
        }
        catch (ArgumentOutOfRangeException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception is not null).IsTrue();
    }

    [Test]
    public async Task AddExperience_ThresholdCountIsNotTen_ThrowsArgumentExceptionAsync()
    {
        // Arrange
        var initial = new SkillProgress(1, 0, 0);
        ArgumentException? exception = null;

        // Act
        try
        {
            initial.AddExperience(1, [0, 8]);
        }
        catch (ArgumentException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception is not null).IsTrue();
    }
}
