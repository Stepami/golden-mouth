using Mediator;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Reki.Application.Common;

namespace Reki.Tests.Common;

public sealed class GameDayTests
{
    private static readonly GameBalance Balance = TestGameData.CreateBalance();

    [Test]
    public async Task PeriodStart_ChangesAtNineOClockMoscowAsync()
    {
        var beforeReset = new GameDay(
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 5, 59, 0, TimeSpan.Zero)),
            Options.Create(Balance),
            Substitute.For<IGameStore>(),
            Substitute.For<ISender>());
        var atReset = new GameDay(
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero)),
            Options.Create(Balance),
            Substitute.For<IGameStore>(),
            Substitute.For<ISender>());

        await Assert.That(beforeReset.PeriodStart)
            .IsEqualTo(new DateTimeOffset(2026, 10, 4, 6, 0, 0, TimeSpan.Zero));
        await Assert.That(atReset.PeriodStart)
            .IsEqualTo(new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public async Task Normalize_ClockCrossesReset_ResetsEnergyOnlyOnceAsync()
    {
        // Arrange
        var clock = new FakeTimeProvider(TestGameData.PeriodStart.AddDays(1).AddTicks(-1));
        var day = new GameDay(clock, Options.Create(Balance), Substitute.For<IGameStore>(), Substitute.For<ISender>());
        var player = TestGameData.CreatePlayer() with { EnergyUsed = 9, DailyBonus = 3, Version = 4 };

        // Act
        var before = day.Normalize(player);
        clock.Advance(TimeSpan.FromTicks(1));
        var atReset = day.Normalize(before);
        clock.Advance(TimeSpan.FromTicks(1));
        var after = day.Normalize(atReset);

        // Assert
        await Assert.That(before.EnergyUsed).IsEqualTo(9);
        await Assert.That(before.Version).IsEqualTo(4);
        await Assert.That(atReset.EnergyUsed).IsEqualTo(0);
        await Assert.That(atReset.DailyBonus).IsEqualTo(0);
        await Assert.That(atReset.PostsPeriodStartedAtUtc).IsEqualTo(TestGameData.PeriodStart.AddDays(1));
        await Assert.That(atReset.Version).IsEqualTo(5);
        await Assert.That(after.Version).IsEqualTo(5);
        await Assert.That(after.EnergyUsed).IsEqualTo(0);
    }
}