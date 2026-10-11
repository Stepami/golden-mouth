using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Reki.Application.Common;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;
using Reki.Tests.Common;

namespace Reki.Tests.Social;

public sealed class ChallengeLifecycleTests
{
    [Test]
    public async Task StartOrAcceptAsync_ReciprocalInvitation_ActivatesChallengeForBothPlayersAsync()
    {
        // Arrange
        var initiatorId = PlayerId.From("initiator");
        var opponentId = PlayerId.From("opponent");
        var initiator = TestGameData.CreatePlayer() with { Id = initiatorId };
        var opponent = TestGameData.CreatePlayer() with
        {
            Id = opponentId,
            TelegramUserId = TelegramUserId.From(2),
        };
        var pending = GameChallenge.CreatePending(
            initiatorId,
            opponentId,
            TestGameData.PeriodStart.AddHours(-1),
            3,
            5);
        var store = Substitute.For<IGameStore>();
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
        store.GetWorldsAsync(initiatorId, Arg.Any<CancellationToken>()).Returns([
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    PlayerId = initiatorId,
                },
            }
        ]);
        store.GetWorldsAsync(opponentId, Arg.Any<CancellationToken>()).Returns([
            TestGameData.CreateWorld() with
            {
                Player = TestGameData.CreateWorld().Player with
                {
                    PlayerId = opponentId,
                },
            }
        ]);
        var storedPlayers = new List<World>();
        store.TryReplaceWorldAsync(
                Arg.Do<World>(player => storedPlayers.Add(player)),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        var balance = TestGameData.CreateBalance();
        var timeProvider = new FakeTimeProvider(TestGameData.PeriodStart.AddHours(2));
        var gameDay = new GameDay(timeProvider, Options.Create(balance));
        var analytics = new ProductTelemetry(
            Substitute.For<ILogger<ProductTelemetry>>(),
            timeProvider);
        var lifecycle = new ChallengeLifecycle(store, Options.Create(balance), gameDay, timeProvider, analytics);

        // Act
        await lifecycle.StartOrAcceptAsync(opponent, initiatorId, CancellationToken.None);

        // Assert
        var active = storedChallenge ?? throw new InvalidOperationException(
            "The accepted challenge was not persisted.");
        await Assert.That(active.Status).IsEqualTo(ChallengeStatus.Active);
        await Assert.That(active.StartedAtUtc).IsEqualTo(TestGameData.PeriodStart);
        await Assert.That(active.Version).IsEqualTo(1);
        await Assert.That(storedPlayers.Count).IsEqualTo(2);
        await Assert.That(storedPlayers.Select(player => player.Player.PlayerId))
            .IsEquivalentTo([initiatorId, opponentId]);
        await Assert.That(storedPlayers.All(player => player.Player.Achievements
            .Single(achievement => achievement.Id == AchievementCatalog.Challenge)
            .IsOpen)).IsTrue();
    }
}