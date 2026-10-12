using Reki.Domain.Player;
using Reki.Tests.Common;

namespace Reki.Tests.Player;

public sealed class PlayerEnergyTests
{
    [Test]
    [Arguments(0)]
    [Arguments(4)]
    [Arguments(9)]
    public async Task SpendEnergy_OneAction_IncrementsUsageOnceAsync(int used)
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with { EnergyUsed = used, Version = 7 };

        // Act
        var updated = player.SpendEnergy(1, 10);

        // Assert
        await Assert.That(updated.EnergyUsed).IsEqualTo(used + 1);
        await Assert.That(updated.Version).IsEqualTo(8);
        await Assert.That(updated.Skills).IsEqualTo(player.Skills);
        await Assert.That(player.EnergyUsed).IsEqualTo(used);
    }

    [Test]
    [Arguments(10)]
    [Arguments(11)]
    public async Task SpendEnergy_ExhaustedBudget_RejectsWithoutChangingPlayerAsync(int used)
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with { EnergyUsed = used, Version = 7 };

        // Act
        var spend = () => player.SpendEnergy(1, 10);

        // Assert
        await Assert.That(spend).Throws<InvalidOperationException>();
        await Assert.That(player.EnergyUsed).IsEqualTo(used);
        await Assert.That(player.Version).IsEqualTo(7);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task SpendEnergy_NonPositiveCost_RejectsValueAsync(int cost)
    {
        // Arrange
        var player = TestGameData.CreatePlayer();

        // Act
        var spend = () => player.SpendEnergy(cost, 10);

        // Assert
        await Assert.That(spend).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(false, 0, 0, 0, 10)]
    [Arguments(true, 0, 0, 0, 20)]
    [Arguments(false, 3, 5, 1, 18)]
    [Arguments(true, 3, 5, 1, 28)]
    [Arguments(false, 3, 5, 0, 13)]
    [Arguments(false, 3, 5, -1, 13)]
    public async Task GetPostsCap_CombinedAllowances_UsesOnlyActiveChallengeBonusAsync(
        bool plus,
        int dailyBonus,
        int challengeBonus,
        int expiryDays,
        int expected)
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with
        {
            Settings = new PlayerSettings("on", "off", plus),
            DailyBonus = dailyBonus,
            ChallengeDailyBonus = challengeBonus,
            ChallengeBonusUntilUtc = TestGameData.PeriodStart.AddDays(expiryDays),
        };

        // Act
        var cap = player.GetPostsCap(10, 20);

        // Assert
        await Assert.That(cap).IsEqualTo(expected);
    }

    [Test]
    public async Task StartNewPeriod_NewDay_ResetsDailyBudgetAsync()
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with
        {
            EnergyUsed = 8,
            DailyBonus = 3,
            Version = 4,
            PlusInterestClaimed = true,
            ChallengeDailyBonus = 5,
            ChallengeBonusUntilUtc = TestGameData.PeriodStart.AddDays(3),
        };
        var nextDay = TestGameData.PeriodStart.AddDays(1);

        // Act
        var updated = player.StartNewPeriod(nextDay);

        // Assert
        await Assert.That(updated.EnergyUsed).IsEqualTo(0);
        await Assert.That(updated.DailyBonus).IsEqualTo(0);
        await Assert.That(updated.PostsPeriodStartedAtUtc).IsEqualTo(nextDay);
        await Assert.That(updated.Version).IsEqualTo(5);
        await Assert.That(updated.PlusInterestClaimed).IsTrue();
        await Assert.That(updated.GetPostsCap(10, 20)).IsEqualTo(15);
        await Assert.That(updated.Skills).IsEqualTo(player.Skills);
        await Assert.That(updated.StreakDays).IsEqualTo(player.StreakDays);
    }
}
