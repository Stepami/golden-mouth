using Reki.Domain.Player;
using Reki.Tests.Common;

namespace Reki.Tests.Player;

public sealed class PlayerProgressionTests
{
    [Test]
    public async Task Create_ForWorld_ReturnsExactlyTwelveClosedAchievementsAsync()
    {
        // Arrange
        string[] expectedIds =
        [
            AchievementCatalog.StarCard,
            AchievementCatalog.StarStory,
            AchievementCatalog.FirstViral,
            AchievementCatalog.HateSurvived,
            AchievementCatalog.Collaboration,
            AchievementCatalog.TenThousand,
            AchievementCatalog.SkillMastery,
            AchievementCatalog.ProfileFrame,
            AchievementCatalog.Secret,
            AchievementCatalog.Duet,
            AchievementCatalog.Challenge,
            AchievementCatalog.Rematch,
        ];

        // Act
        var achievements =
            AchievementCatalog.Create(TestGameData.WorldId);

        // Assert
        await Assert.That(achievements.Count).IsEqualTo(12);
        await Assert.That(achievements.Select(item => item.Id)).IsEquivalentTo(expectedIds);
        await Assert.That(achievements.All(item => item.WorldId == TestGameData.WorldId)).IsTrue();
        await Assert.That(achievements.All(item => !item.IsOpen)).IsTrue();
    }

    [Test]
    public async Task PublishPost_FirstViral_UnlocksFirstViralAchievementAsync()
    {
        // Arrange
        var player = TestGameData.CreatePlayer();

        // Act
        var result = Publish(player, 1, true, player.Skills, null);

        // Assert
        await Assert.That(IsOpen(result, AchievementCatalog.FirstViral)).IsTrue();
        await Assert.That(IsOpen(result, AchievementCatalog.TenThousand)).IsFalse();
    }

    [Test]
    public async Task PublishPost_ReachesTenThousandSubscribers_UnlocksTenThousandAchievementAsync()
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with { Subscribers = 9_999 };

        // Act
        var result = Publish(player, 1, false, player.Skills, null);

        // Assert
        await Assert.That(result.Subscribers).IsEqualTo(10_000);
        await Assert.That(IsOpen(result, AchievementCatalog.TenThousand)).IsTrue();
        await Assert.That(IsOpen(result, AchievementCatalog.FirstViral)).IsFalse();
    }

    [Test]
    public async Task PublishPost_ReachesSkillLevelThree_UnlocksMasteryAchievementAsync()
    {
        // Arrange
        var skills = new Dictionary<SkillId, SkillProgress>(TestGameData.CreateSkills())
        {
            [TestGameData.Brave] = new(3, 0, 20),
        };
        var player = TestGameData.CreatePlayer();

        // Act
        var result = Publish(player, 0, false, skills, null);

        // Assert
        await Assert.That(result.Skills[TestGameData.Brave].Level).IsEqualTo(3);
        await Assert.That(IsOpen(result, AchievementCatalog.SkillMastery)).IsTrue();
        await Assert.That(IsOpen(result, AchievementCatalog.TenThousand)).IsFalse();
    }

    [Test]
    public async Task PublishPost_TwoCompletedWantsInOnePeriod_GainsOnlyOneRelationPointAsync()
    {
        // Arrange
        var player = TestGameData.CreatePlayer() with
        {
            Wants =
            [
                new DailyWant("want-one", "Первое желание", "+1", false),
                new DailyWant("want-two", "Второе желание", "+1", false),
            ],
        };

        // Act
        var afterFirst = Publish(player, 0, false, player.Skills, "want-one");
        var afterSecond = Publish(afterFirst, 0, false, afterFirst.Skills, "want-two");
        var relation = afterSecond.StarRelations.Single();

        // Assert
        await Assert.That(afterSecond.Wants.All(want => want.Done)).IsTrue();
        await Assert.That(relation.Value).IsEqualTo(1);
        await Assert.That(relation.GainedInPeriod).IsEqualTo(1);
        await Assert.That(relation.PeriodStartedAtUtc).IsEqualTo(TestGameData.PeriodStart);
    }

    private static PlayerProfile Publish(
        PlayerProfile player,
        long subscriberDelta,
        bool viral,
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        string? completedWantId)
    {
        return player.PublishPost(
            subscriberDelta,
            viral,
            false,
            skills,
            completedWantId,
            TestGameData.StarId,
            TestGameData.PeriodStart.AddHours(1),
            -5,
            5,
            1,
            3);
    }

    private static bool IsOpen(PlayerProfile player, string achievementId)
    {
        return player.Achievements.Single(item => item.Id == achievementId).IsOpen;
    }
}
