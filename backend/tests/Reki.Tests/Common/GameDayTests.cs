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
}
