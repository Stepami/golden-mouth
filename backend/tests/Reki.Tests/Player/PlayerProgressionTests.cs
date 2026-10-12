using Reki.Domain.Player;
using Reki.Domain.Worlds;
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
            AchievementCatalog.Create();

        // Assert
        await Assert.That(achievements.Count).IsEqualTo(12);
        await Assert.That(achievements.Select(item => item.Id)).IsEquivalentTo(expectedIds);
        await Assert.That(achievements.All(item => !item.IsOpen)).IsTrue();
    }

    [Test]
    public async Task PublishPost_FirstViral_UnlocksFirstViralAchievementAsync()
    {
        // Arrange
        var player = TestGameData.CreateWorld();

        // Act
        var result = Publish(player, 1, true, TestGameData.CreateSkills(), null);

        // Assert
        await Assert.That(IsOpen(result, AchievementCatalog.FirstViral)).IsTrue();
        await Assert.That(IsOpen(result, AchievementCatalog.TenThousand)).IsFalse();
    }

    [Test]
    public async Task PublishPost_ReachesTenThousandSubscribers_UnlocksTenThousandAchievementAsync()
    {
        // Arrange
        var player = TestGameData.CreateWorld() with
        {
            Player = TestGameData.CreateWorld().Player with
            {
                Subscribers = 9_999,
            },
        };

        // Act
        var result = Publish(player, 1, false, TestGameData.CreateSkills(), null);

        // Assert
        await Assert.That(result.Player.Subscribers).IsEqualTo(10_000);
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
        var player = TestGameData.CreateWorld();

        // Act
        var result = Publish(player, 0, false, skills, null);

        // Assert
        await Assert.That(skills[TestGameData.Brave].Level).IsEqualTo(3);
        await Assert.That(IsOpen(result, AchievementCatalog.SkillMastery)).IsTrue();
        await Assert.That(IsOpen(result, AchievementCatalog.TenThousand)).IsFalse();
    }

    [Test]
    public async Task PublishPost_TwoCompletedWantsInOnePeriod_GainsOnlyOneRelationPointAsync()
    {
        // Arrange
        var player = TestGameData.CreateWorld() with
        {
            Player = TestGameData.CreateWorld().Player with
            {
                Wants = [
                new DailyWant("want-one", "Первое желание", "+1", false),
                new DailyWant("want-two", "Второе желание", "+1", false),
            ],
            },
        };

        // Act
        var afterFirst = Publish(player, 0, false, TestGameData.CreateSkills(), "want-one");
        var afterSecond = Publish(afterFirst, 0, false, TestGameData.CreateSkills(), "want-two");
        var relation = afterSecond.Stars.Single();

        // Assert
        await Assert.That(afterSecond.Player.Wants.All(want => want.Done)).IsTrue();
        await Assert.That(relation.Relation).IsEqualTo(1);
        await Assert.That(relation.RelationGainedInPeriod).IsEqualTo(1);
        await Assert.That(relation.RelationPeriodStartedAtUtc).IsEqualTo(TestGameData.PeriodStart);
    }

    private static World Publish(
        World player,
        long subscriberDelta,
        bool viral,
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        string? completedWantId)
    {
        return player.PublishPost(
            subscriberDelta,
            viral,
            skills,
            completedWantId,
            TestGameData.StarId,
            TestGameData.PeriodStart.AddHours(1),
            -5,
            5,
            1,
            3);
    }

    private static bool IsOpen(World player, string achievementId)
    {
        return player.Player.Achievements.Single(item => item.Id == achievementId).IsOpen;
    }
}