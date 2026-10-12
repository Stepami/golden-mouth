using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Tests.Common;

namespace Reki.Tests.Social;

public sealed class GameChallengeTests
{
    private static readonly PlayerId InitiatorId = PlayerId.From("initiator");
    private static readonly PlayerId OpponentId = PlayerId.From("opponent");

    [Test]
    public async Task Score_TwoPosts_UsesSeventyPercentQualityPlusThirtyPercentReachRatioAsync()
    {
        // Arrange
        var initial = ChallengePerformance.Empty;

        // Act
        var result = initial.Add(0.8, 2).Add(0.6, 1);

        // Assert
        await Assert.That(result.PostCount).IsEqualTo(2);
        await Assert.That(result.QualityTotal).IsEqualTo(1.4);
        await Assert.That(result.ReachRatioTotal).IsEqualTo(3);
        await Assert.That(Math.Round(result.Score, 4)).IsEqualTo(9.4);
    }

    [Test]
    public async Task Add_OutOfRangeMetrics_ClampsToDomainBoundsAsync()
    {
        // Arrange
        var initial = ChallengePerformance.Empty;

        // Act
        var result = initial.Add(1.5, -2);

        // Assert
        await Assert.That(result.QualityTotal).IsEqualTo(1);
        await Assert.That(result.ReachRatioTotal).IsEqualTo(0);
        await Assert.That(result.Score).IsEqualTo(7);
    }

    [Test]
    public async Task RecordPost_DifferentAbsoluteReachOrSubscriberDelta_UsesNormalizedRatioOnlyAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        var smallChannelPost = CreatePost(
            InitiatorId,
            "Малый канал",
            120,
            12,
            0.8,
            1.2);
        var largeChannelPost = CreatePost(
            OpponentId,
            "Большой канал",
            120_000,
            12_000,
            0.8,
            1.2);

        // Act
        var result = challenge
            .RecordPost(InitiatorId, smallChannelPost)
            .RecordPost(OpponentId, largeChannelPost);

        // Assert
        await Assert.That(result.InitiatorPerformance.Score)
            .IsEqualTo(result.OpponentPerformance.Score);
        await Assert.That(result.InitiatorPerformance.ReachRatioTotal).IsEqualTo(1.2);
        await Assert.That(result.OpponentPerformance.ReachRatioTotal).IsEqualTo(1.2);
    }

    [Test]
    public async Task RecordPost_HigherNormalizedScore_TracksBestPostAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        var lower = CreatePost(InitiatorId, "Первый", 100, 10, 0.4, 0.5);
        var higher = CreatePost(OpponentId, "Лучший", 80, 5, 0.9, 1.5);

        // Act
        var result = challenge
            .RecordPost(InitiatorId, lower)
            .RecordPost(OpponentId, higher);

        // Assert
        await Assert.That(result.BestPost).IsEqualTo("Лучший");
        await Assert.That(result.BestPostScore)
            .IsEqualTo(ChallengePerformance.CalculatePostScore(0.9, 1.5));
    }

    [Test]
    public async Task CreatePending_SameParticipant_ThrowsArgumentExceptionAsync()
    {
        // Arrange
        ArgumentException? exception = null;

        // Act
        try
        {
            GameChallenge.CreatePending(
                InitiatorId,
                InitiatorId,
                TestGameData.PeriodStart,
                3,
                5);
        }
        catch (ArgumentException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception is not null).IsTrue();
    }

    [Test]
    public async Task Accept_InitiatorAttemptsAcceptance_ThrowsInvalidOperationExceptionAsync()
    {
        // Arrange
        var pending = GameChallenge.CreatePending(
            InitiatorId,
            OpponentId,
            TestGameData.PeriodStart.AddHours(-1),
            3,
            5);
        InvalidOperationException? exception = null;

        // Act
        try
        {
            pending.Accept(InitiatorId, TestGameData.PeriodStart);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception is not null).IsTrue();
        await Assert.That(pending.Status).IsEqualTo(ChallengeStatus.Pending);
    }

    [Test]
    public async Task GetDay_AtFinishBoundary_ReportsFinalDayThenFinishedAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();

        // Act
        var firstDay = challenge.GetDay(TestGameData.PeriodStart);
        var thirdDay = challenge.GetDay(TestGameData.PeriodStart.AddDays(2));
        var beforeFinish = challenge.IsFinished(TestGameData.PeriodStart.AddDays(2));
        var atFinish = challenge.IsFinished(TestGameData.PeriodStart.AddDays(3));

        // Assert
        await Assert.That(firstDay).IsEqualTo(1);
        await Assert.That(thirdDay).IsEqualTo(3);
        await Assert.That(beforeFinish).IsFalse();
        await Assert.That(atFinish).IsTrue();
    }

    [Test]
    public async Task Complete_OnlyOpponentHasFivePosts_SelectsEligibleOpponentAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        challenge = AddPosts(challenge, InitiatorId, 4, 1, 2);
        challenge = AddPosts(challenge, OpponentId, 5, 0.1, 0.1);

        // Act
        var result = challenge.Complete();

        // Assert
        await Assert.That(result.InitiatorPerformance.PostCount).IsEqualTo(4);
        await Assert.That(result.OpponentPerformance.PostCount).IsEqualTo(5);
        await Assert.That(result.WinnerId).IsEqualTo(OpponentId);
        await Assert.That(result.Status).IsEqualTo(ChallengeStatus.Completed);
    }

    [Test]
    public async Task Complete_BothHaveFivePosts_SelectsHigherNormalizedScoreAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        challenge = AddPosts(challenge, InitiatorId, 5, 0.9, 1.5);
        challenge = AddPosts(challenge, OpponentId, 5, 0.5, 0.5);

        // Act
        var result = challenge.Complete();

        // Assert
        await Assert.That(result.InitiatorPerformance.Score)
            .IsGreaterThan(result.OpponentPerformance.Score);
        await Assert.That(result.WinnerId).IsEqualTo(InitiatorId);
        await Assert.That(result.Version).IsEqualTo(challenge.Version + 1);
    }

    [Test]
    public async Task Complete_NeitherHasFivePosts_LeavesWinnerEmptyAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        challenge = AddPosts(challenge, InitiatorId, 4, 1, 2);
        challenge = AddPosts(challenge, OpponentId, 4, 0.1, 0.1);

        // Act
        var result = challenge.Complete();

        // Assert
        await Assert.That(result.WinnerId).IsNull();
        await Assert.That(result.Status).IsEqualTo(ChallengeStatus.Completed);
    }

    [Test]
    public async Task Complete_EligibleScoresAreEqual_LeavesWinnerEmptyAsync()
    {
        // Arrange
        var challenge = CreateActiveChallenge();
        challenge = AddPosts(challenge, InitiatorId, 5, 0.7, 1);
        challenge = AddPosts(challenge, OpponentId, 5, 0.7, 1);

        // Act
        var result = challenge.Complete();

        // Assert
        await Assert.That(result.InitiatorPerformance.Score)
            .IsEqualTo(result.OpponentPerformance.Score);
        await Assert.That(result.WinnerId).IsNull();
    }

    private static GameChallenge CreateActiveChallenge()
    {
        return GameChallenge.CreatePending(
                InitiatorId,
                OpponentId,
                TestGameData.PeriodStart.AddHours(-1),
                3,
                5)
            .Accept(OpponentId, TestGameData.PeriodStart);
    }

    private static GameChallenge AddPosts(
        GameChallenge challenge,
        PlayerId playerId,
        int count,
        double quality,
        double reachRatio)
    {
        var current = challenge;
        for (var index = 0; index < count; index++)
        {
            current = current.RecordPost(
                playerId,
                CreatePost(
                    playerId,
                    $"Пост {index + 1}",
                    100,
                    10,
                    quality,
                    reachRatio));
        }

        return current;
    }

    private static GamePost CreatePost(
        PlayerId playerId,
        string text,
        long views,
        long subscriberDelta,
        double quality,
        double reachRatio)
    {
        return new GamePost(
            PostId.New(),
            playerId,
            TestGameData.CreateWorld().Id,
            text,
            views,
            [],
            [],
            TestGameData.PeriodStart,
            subscriberDelta,
            quality,
            reachRatio,
            false,
            false);
    }
}