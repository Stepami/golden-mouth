using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Reki.Application.Common;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Tests.Common;

namespace Reki.Tests.Social;

public sealed class ChallengeLifecycleTests
{
    [Test]
    public async Task StartOrAcceptAsync_ReciprocalInvitation_ActivatesChallengeForBothPlayersAsync()
    {
        // Arrange
        PlayerId initiatorId = PlayerId.From("initiator");
        PlayerId opponentId = PlayerId.From("opponent");
        PlayerProfile initiator = TestGameData.CreatePlayer() with { Id = initiatorId };
        PlayerProfile opponent = TestGameData.CreatePlayer() with
        {
            Id = opponentId,
            TelegramUserId = TelegramUserId.From(2),
        };
        GameChallenge pending = GameChallenge.CreatePending(
            initiatorId,
            opponentId,
            TestGameData.PeriodStart.AddHours(-1),
            3,
            5);
        IGameStore store = Substitute.For<IGameStore>();
        store.GetPlayerAsync(initiatorId, Arg.Any<CancellationToken>()).Returns(initiator);
        store.GetPlayerAsync(opponentId, Arg.Any<CancellationToken>()).Returns(opponent);
        store.GetPendingChallengeAsync(
                opponentId,
                initiatorId,
                Arg.Any<CancellationToken>())
            .Returns(pending);
        store.GetActiveChallengeAsync(initiatorId, Arg.Any<CancellationToken>())
            .Returns((GameChallenge?)null);
        store.GetActiveChallengeAsync(opponentId, Arg.Any<CancellationToken>())
            .Returns((GameChallenge?)null);
        GameChallenge? storedChallenge = null;
        store.TryReplaceChallengeAsync(
                Arg.Do<GameChallenge>(challenge => storedChallenge = challenge),
                pending.Version,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var storedPlayers = new List<PlayerProfile>();
        store.TryReplacePlayerAsync(
                Arg.Do<PlayerProfile>(player => storedPlayers.Add(player)),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        GameBalance balance = TestGameData.CreateBalance();
        var timeProvider = new FakeTimeProvider(TestGameData.PeriodStart.AddHours(2));
        var gameDay = new GameDay(timeProvider, balance);
        var analytics = new ServerAnalyticsWriter(store, timeProvider);
        var lifecycle = new ChallengeLifecycle(store, balance, gameDay, timeProvider, analytics);

        // Act
        await lifecycle.StartOrAcceptAsync(opponent, initiatorId, CancellationToken.None);

        // Assert
        GameChallenge active = storedChallenge ?? throw new InvalidOperationException(
            "The accepted challenge was not persisted.");
        await Assert.That(active.Status).IsEqualTo(ChallengeStatus.Active);
        await Assert.That(active.StartedAtUtc).IsEqualTo(TestGameData.PeriodStart);
        await Assert.That(active.Version).IsEqualTo(1);
        await Assert.That(storedPlayers.Count).IsEqualTo(2);
        await Assert.That(storedPlayers.Select(player => player.Id))
            .IsEquivalentTo([initiatorId, opponentId]);
        await Assert.That(storedPlayers.All(player => player.Achievements
            .Single(achievement => achievement.Id == AchievementCatalog.Challenge)
            .IsOpen)).IsTrue();
    }
}
