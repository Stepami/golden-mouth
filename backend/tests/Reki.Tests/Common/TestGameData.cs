using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Tests.Common;

internal static class TestGameData
{
    private static readonly StarId StarId = StarId.From("mira");

    internal static World CreateWorld() => new(
        WorldId.From(Guid.Parse("1c6b59c8-9abe-434b-929a-3db01b8d1ab8")),
        new WorldInterface(),
        new WorldPlayer(PlayerId.From(Guid.Parse("31eb7a8322194c548f535274fdf67b2f")), "neutral", WorldDrama.Mid, "Канал", "creator", "avatar", 1000, AchievementCatalog.Create()),
        null,
        WorldDifficulty.Easy,
        false,
        [],
        [],
        [],
        [],
        [],
        [new WorldStar(StarId, "Мира", "лидер BLESK", StarTrait.Boldness, "Ценит смелость", "коротко", "mira")],
        0);
}
