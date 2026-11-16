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

    [MapValue(nameof(World.Id), Use = "Reki.Domain.Worlds.WorldId.New")]
    [MapValue(nameof(World.IsArchived), false)]
    [MapValue(nameof(World.Version), 0)]
    [MapPropertyFromSource(nameof(World.Interface), Use = nameof(ToWorldInterface))]
    [MapPropertyFromSource(nameof(World.Player), Use = nameof(ToWorldPlayer))]
    [MapProperty(nameof(CreateWorldRequest.Difficulty), nameof(World.Difficulty), Use = nameof(MapDifficulty))]
    [MapProperty(nameof(CreateWorldRequest.Rules), nameof(World.Rules), Use = nameof(MapRules))]
    public static partial World ToWorld(this CreateWorldRequest source);

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

    [MapValue(nameof(WorldPlayer.PlayerId), Use = "Reki.Domain.Player.PlayerId.Zero")]
    [MapValue(nameof(WorldPlayer.Subscribers), 0L)]
    [MapperIgnoreSource(nameof(CreateWorldRequest.TemplateId))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Title))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Description))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.AvatarStyle))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Where))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.FirstContext))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.FirstPrompt))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Emoji))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Summary))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Difficulty))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.UnlockSkill))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.UnlockLevel))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voices))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.PostIdeas))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.DemoPosts))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Roles))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Rules))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Stars))]
    private static partial WorldPlayer ToWorldPlayer(this CreateWorldRequest source);

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
