using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Tests.Common;

internal static class TestGameData
{
    internal static readonly WorldId WorldId = WorldId.From("stage");
    internal static readonly StarId StarId = StarId.From("mira");
    internal static readonly SkillId Brave = SkillId.From("brave");
    internal static readonly SkillId Humor = SkillId.From("humor");
    internal static readonly SkillId Topic = SkillId.From("topic");
    internal static readonly DateTimeOffset PeriodStart =
        new(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);

    internal static GameBalance CreateBalance()
    {
        return new GameBalance(
            10,
            20,
            new TimeOnly(9, 0),
            5,
            3,
            5,
            3,
            3,
            new GrowthBalance(
                0.6,
                1,
                [new(1, 5, 1), new(6, 10, 0.7), new(11, 99, 0.25)],
                1000,
                2,
                1.3,
                2,
                3,
                2,
                120),
            new ViralBalance(0.03, 0.06, 3, 15, 3),
            new FlopBalance(2, -0.3),
            new SkillBalance(
                [0, 8, 20, 36, 56, 80, 110, 150, 200, 270],
                1,
                0.15,
                10,
                2,
                3,
                1.3,
                1.3),
            new RelationBalance(-5, 5, 1, 3, 5),
            new ChallengeBalance(3, 5),
            new ThreadBalance(3, 9, 2),
            new Dictionary<string, WorldBalance>
            {
                [WorldId.Value] = new(1, 0.1, 1, long.MaxValue),
            });
    }

    internal static PlayerProfile CreatePlayer()
    {
        return new PlayerProfile(
            PlayerId.From("player-one"),
            TelegramUserId.From(1),
            "18+",
            "Канал",
            "channel",
            "creator",
            "avatar",
            "address",
            WorldId,
            1000,
            0,
            0,
            PeriodStart,
            false,
            1,
            new PlayerSettings("mid", "on", "off", "neutral", false),
            CreateSkills(),
            null,
            null,
            0,
            [new DailyWant("want-one", "Сделать пост", "+1", false)],
            AchievementCatalog.Create(WorldId),
            [],
            null,
            0,
            10,
            0,
            0,
            0,
            null);
    }

    internal static IReadOnlyDictionary<SkillId, SkillProgress> CreateSkills()
    {
        return new Dictionary<SkillId, SkillProgress>
        {
            [Brave] = new(1, 0, 0),
            [Humor] = new(1, 0, 0),
            [Topic] = new(1, 0, 0),
        };
    }

    internal static GeneratedPostTurn CreateTurn(
        double brave = 0.5,
        double humor = 0.5,
        double topic = 0.5)
    {
        return new GeneratedPostTurn(
            "Мира",
            StarId,
            "Комментарий",
            "Хук",
            "Реакция",
            brave,
            humor,
            topic,
            [],
            true);
    }
}
