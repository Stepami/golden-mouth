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

    public static UpdateWorld ToUpdateWorld(this CreateWorldRequest source, string worldId) =>
        new(WorldId.From(worldId), source.ToWorld());

    // These source fields are projected by the dedicated Interface and Player mappings.
    [MapperIgnoreSource(nameof(CreateWorldRequest.Name))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Avatar))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Role))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Title))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Description))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.AvatarStyle))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voice))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Drama))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Where))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.FirstContext))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.FirstPrompt))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Emoji))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Summary))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Difficulty))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.UnlockSkill))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.UnlockLevel))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Wants))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Achievements))]
    [MapPropertyFromSource(nameof(World.Interface), Use = nameof(ToWorldInterface))]
    [MapPropertyFromSource(nameof(World.Player), Use = nameof(ToWorldPlayer))]
    [MapValue(nameof(World.Id), Use = nameof(NewWorldId))]
    [MapValue(nameof(World.Version), 0)]
    [MapProperty(nameof(CreateWorldRequest.Rules), nameof(World.Rules), Use = nameof(MapRules))]
    public static partial World ToWorld(this CreateWorldRequest source);

    // Project only interface settings from the flat HTTP request.
    [MapperIgnoreSource(nameof(CreateWorldRequest.BalanceKey))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Name))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Avatar))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Role))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voice))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Drama))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Voices))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.PostIdeas))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.DemoPosts))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Wants))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Achievements))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Roles))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Rules))]
    [MapperIgnoreSource(nameof(CreateWorldRequest.Stars))]
    [MapProperty(nameof(CreateWorldRequest.Difficulty), nameof(WorldInterface.Difficulty), Use = nameof(MapWorldDifficulty))]
    private static partial WorldInterface ToWorldInterface(this CreateWorldRequest source);

    // Project player settings and initialize fresh progress independently of submitted values.
    [MapperIgnoreSource(nameof(CreateWorldRequest.BalanceKey))]
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
    [MapValue(nameof(WorldPlayer.PlayerId), Use = nameof(InitialPlayerId))]
    [MapValue(nameof(WorldPlayer.Subscribers), 0L)]
    [MapValue(nameof(WorldPlayer.LastPostAtUtc), null)]
    [MapValue(nameof(WorldPlayer.RestedPostsRemaining), 0)]
    [MapValue(nameof(WorldPlayer.PeriodStartedAtUtc), Use = nameof(InitialPlayerPeriod))]
    private static partial WorldPlayer ToWorldPlayer(this CreateWorldRequest source);

    private static WorldId NewWorldId() => WorldId.New();

    private static PlayerId InitialPlayerId() => PlayerId.From("00000000000000000000000000000000");

    private static DateTimeOffset InitialPlayerPeriod() => default;

    private static IReadOnlyList<WorldRule> MapRules(IReadOnlyList<WorldRuleRequest> source) =>
    [
        .. source
            .Where(rule => rule.Text != "Без тем 18+ и травли по внешности")
            .Select(rule => new WorldRule(rule.Text, false)),
        new("Без тем 18+ и травли по внешности", true)
    ];

    [MapperIgnoreSource(nameof(WorldStarRequest.Relation))]
    [MapValue(nameof(WorldStar.Relation), 0)]
    [MapValue(nameof(WorldStar.RelationGainedInPeriod), 0)]
    [MapValue(nameof(WorldStar.RelationPeriodStartedAtUtc), Use = nameof(InitialRelationPeriod))]
    private static partial WorldStar MapStar(WorldStarRequest source);

    [MapperIgnoreSource(nameof(WantRequest.Done))]
    [MapValue(nameof(DailyWant.Done), false)]
    private static partial DailyWant MapWant(WantRequest source);

    [MapperIgnoreSource(nameof(CollectionCardRequest.Open))]
    [MapValue(nameof(AchievementProgress.IsOpen), false)]
    private static partial AchievementProgress MapAchievement(CollectionCardRequest source);

    private static DateTimeOffset InitialRelationPeriod() => DateTimeOffset.UnixEpoch;

    private static StarId MapStarId(string source) => StarId.From(source);

    private static WorldDifficulty MapWorldDifficulty(int source) => source switch
    {
        1 => WorldDifficulty.Easy,
        2 => WorldDifficulty.Normal,
        3 => WorldDifficulty.Hard,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    private static StarTrait MapStarTrait(int source) =>
        Enum.Parse<StarTrait>(source.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
