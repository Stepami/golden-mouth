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

    [MapValue(nameof(World.Id), Use = nameof(NewWorldId))]
    [MapValue(nameof(World.Version), 0)]
    [MapProperty(nameof(CreateWorldRequest.Rules), nameof(World.Rules), Use = nameof(MapRules))]
    [MapProperty(nameof(CreateWorldRequest.Title), "Interface.Title")]
    [MapProperty(nameof(CreateWorldRequest.Description), "Interface.Description")]
    [MapProperty(nameof(CreateWorldRequest.AvatarStyle), "Interface.AvatarStyle")]
    [MapProperty(nameof(CreateWorldRequest.Where), "Interface.Where")]
    [MapProperty(nameof(CreateWorldRequest.FirstContext), "Interface.FirstContext")]
    [MapProperty(nameof(CreateWorldRequest.FirstPrompt), "Interface.FirstPrompt")]
    [MapProperty(nameof(CreateWorldRequest.Emoji), "Interface.Emoji")]
    [MapProperty(nameof(CreateWorldRequest.Summary), "Interface.Summary")]
    [MapProperty(nameof(CreateWorldRequest.UnlockSkill), "Interface.UnlockSkill")]
    [MapProperty(nameof(CreateWorldRequest.UnlockLevel), "Interface.UnlockLevel")]
    [MapProperty(nameof(CreateWorldRequest.Difficulty), "Interface.Difficulty", Use = nameof(MapWorldDifficulty))]
    [MapProperty(nameof(CreateWorldRequest.Name), "Player.Name")]
    [MapProperty(nameof(CreateWorldRequest.Avatar), "Player.Avatar")]
    [MapProperty(nameof(CreateWorldRequest.Role), "Player.Role")]
    [MapProperty(nameof(CreateWorldRequest.Voice), "Player.Voice")]
    [MapProperty(nameof(CreateWorldRequest.Drama), "Player.Drama")]
    [MapProperty(nameof(CreateWorldRequest.Wants), "Player.Wants")]
    [MapProperty(nameof(CreateWorldRequest.Achievements), "Player.Achievements")]
    [MapValue("Player.PlayerId", Use = nameof(InitialPlayerId))]
    [MapValue("Player.Subscribers", 0L)]
    [MapValue("Player.LastPostAtUtc", null)]
    [MapValue("Player.RestedPostsRemaining", 0)]
    [MapValue("Player.PeriodStartedAtUtc", Use = nameof(InitialPlayerPeriod))]
    public static partial World ToWorld(this CreateWorldRequest source);

    private static WorldId NewWorldId() => WorldId.New();

    private static PlayerId InitialPlayerId() => PlayerId.From("00000000000000000000000000000000");

    private static DateTimeOffset InitialPlayerPeriod() => default;

    private static IReadOnlyList<WorldRule> MapRules(IReadOnlyList<WorldRuleRequest> source) =>
        [.. source.Where(rule => rule.Text != "Без тем 18+ и травли по внешности")
            .Select(rule => new WorldRule(rule.Text, false)), new WorldRule("Без тем 18+ и травли по внешности", true)];

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
