using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Tests.Common;

internal static class TestGameData
{
    private static readonly StarId StarId = StarId.From("mira");
    private static readonly DateTimeOffset PeriodStart = new(2026, 10, 5, 6, 0, 0, TimeSpan.Zero);

    internal static World CreateWorld() => new()
    {
        Id = WorldId.From(Guid.Parse("1c6b59c8-9abe-434b-929a-3db01b8d1ab8")),
        Difficulty = WorldDifficulty.Easy,
        Interface = new(),
        Stars = [new WorldStar(StarId, "Мира", "лидер BLESK", StarTrait.Boldness, "Ценит смелость", "коротко", "mira", 0, 0, PeriodStart)],
        Player = new()
        {
            PlayerId = PlayerId.From(Guid.Parse("31eb7a8322194c548f535274fdf67b2f")),
            Name = "Канал",
            Role = "creator",
            Avatar = "avatar",
            Subscribers = 1000,
            Drama = WorldDrama.Mid,
            Voice = "neutral",
            Wants = [new DailyWant("want-one", "Сделать пост", "+1", false)],
            Achievements = AchievementCatalog.Create(),
        },
    };
}
