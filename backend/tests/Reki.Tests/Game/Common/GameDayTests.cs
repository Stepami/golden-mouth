using Reki.Application.Game.Common;

namespace Reki.Tests.Game.Common;

public sealed class GameDayTests
{
    private static readonly GameBalance Balance = new(
        10,
        20,
        new TimeOnly(9, 0),
        5,
        120,
        5);

    [Test]
    public async Task PeriodStart_ChangesAtNineOClockMoscowAsync()
    {
        var beforeReset = new GameDay(
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 5, 5, 59, 0, TimeSpan.Zero)),
            Balance);
        var atReset = new GameDay(
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero)),
            Balance);

        await Assert.That(beforeReset.PeriodStart)
            .IsEqualTo(new DateTimeOffset(2026, 10, 4, 6, 0, 0, TimeSpan.Zero));
        await Assert.That(atReset.PeriodStart)
            .IsEqualTo(new DateTimeOffset(2026, 10, 5, 6, 0, 0, TimeSpan.Zero));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
