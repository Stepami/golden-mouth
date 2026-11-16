using Reki.Application.Worlds.CreateWorldCommand;
using Reki.Application.Worlds.UpdateWorldCommand;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Worlds;

[Mapper]
internal static partial class WorldRequestMapper
{
    public static CreateWorld ToCreateWorld(this CreateWorldRequest source) => new(source.ToWorld());

    public static UpdateWorld ToUpdateWorld(this CreateWorldRequest source, WorldId worldId) =>
        new(worldId, source.ToWorld());

    public static World ToWorld(this CreateWorldRequest source) => new(
        WorldId.New(),
        source.ToWorldInterface(),
        source.ToWorldPlayer(),
        source.TemplateId,
        MapDifficulty(source.Difficulty),
        false,
        source.Roles,
        source.Voices,
        source.PostIdeas,
        source.DemoPosts,
        MapRules(source.Rules),
        source.Stars.Select(MapStar).ToArray(),
        0);

    // Project only interface settings from the flat HTTP request.
    [MapperIgnoreSource(nameof(CreateWorldRequest.TemplateId))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Name))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Avatar))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Role))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voice))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Drama))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voices))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.PostIdeas))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.DemoPosts))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Achievements))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Roles))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Rules))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Stars))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Difficulty))]
    private static partial WorldInterface ToWorldInterface(this CreateWorldRequest source);

    private static WorldPlayer ToWorldPlayer(this CreateWorldRequest source) => new(
        PlayerId.From(Guid.Empty),
        source.Voice,
        source.Drama,
        source.Name,
        source.Role,
        source.Avatar,
        0,
        source.Achievements.Select(MapAchievement).ToArray());

    private static IReadOnlyList<WorldRule> MapRules(IReadOnlyList<WorldRuleRequest> source) =>
    [
        .. source
            .Where(rule => rule.Text != "Без тем 18+ и травли по внешности")
            .Select(rule => new WorldRule(rule.Text, false)),
        new("Без тем 18+ и травли по внешности", true),
    ];

    private static partial WorldStar MapStar(WorldStarRequest source);

    [MapperIgnoreSource(nameof(CollectionCardRequest.Open))]
    [MapValue(nameof(AchievementProgress.IsOpen), false)]
    private static partial AchievementProgress MapAchievement(CollectionCardRequest source);

    private static WorldDifficulty MapDifficulty(WorldDifficulty source) => source switch
    {
        WorldDifficulty.Easy => WorldDifficulty.Easy,
        WorldDifficulty.Hard => WorldDifficulty.Hard,
        _ => WorldDifficulty.Normal,
    };

    private static StarTrait MapStarTrait(int source) =>
        Enum.Parse<StarTrait>(source.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
