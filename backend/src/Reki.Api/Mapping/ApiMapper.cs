using System.Text.Json;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;
using DomainEventId = Reki.Domain.Events.EventId;

namespace Reki.Api.Mapping;

[Mapper]
public sealed partial class ApiMapper
{
    public DmResponse MapDirectMessages(DirectMessageView view)
    {
        var persona = view.Star;
        StarResponse star = new(persona.Id.Value, persona.Name, persona.Role, persona.Trait.ToString(), persona.About, view.Relation);
        return new DmResponse(
            star,
            "на связи",
            view.Conversation.Memory,
            view.Conversation.Messages
                .Select(MapDirectMessage)
                .ToArray(),
            ["спросить про новый пост", "обсудить событие дня"],
            view.RepliesLeft);
    }

    public AnalyticsSubmission MapAnalytics(AnalyticsBatchRequest source)
    {
        return new AnalyticsSubmission(
            SessionId.From(source.Session),
            source.Platform,
            source.LaunchSurface,
            source.Experiments.ToDictionary(
                item => item.Key,
                item => SanitizeJsonString(item.Value, 64)),
            source.Events.Select(item => new AnalyticsEntry(
                item.WorldId is null ? null : Domain.Worlds.WorldId.From(item.WorldId),
                item.Name,
                item.Timestamp,
                item.Screen,
                item.Prev,
                SanitizeProperties(item.Props))).ToArray());
    }

    public partial SendDirectMessageResponse MapSentDirectMessage(SentDirectMessage source);

    [MapProperty(nameof(FriendSummary.Subscribers), nameof(FriendResponse.Subs))]
    public partial FriendResponse MapFriend(FriendSummary source);

    public partial IReadOnlyList<FriendResponse> MapFriends(IReadOnlyList<FriendSummary> source);

    [MapperIgnoreSource(nameof(DailyGameEvent.PlayerId))]
    [MapperIgnoreSource(nameof(DailyGameEvent.DayStartedAtUtc))]
    [MapperIgnoreSource(nameof(DailyGameEvent.Answer))]
    [MapperIgnoreSource(nameof(DailyGameEvent.WorldId))]
    public partial GameEventResponse MapEvent(DailyGameEvent source);

    public WorldResponse MapWorld(World source) => new()
    {
        Id = source.Id.Value,
        BalanceKey = source.BalanceKey,
        Title = source.Interface.Title,
        Description = source.Interface.Description,
        AvatarStyle = source.Interface.AvatarStyle,
        Emoji = source.Interface.Emoji,
        Summary = source.Interface.Summary,
        Difficulty = Convert.ToInt32(source.Interface.Difficulty),
        UnlockSkill = source.Interface.UnlockSkill,
        UnlockLevel = source.Interface.UnlockLevel,
        Where = source.Interface.Where,
        FirstContext = source.Interface.FirstContext,
        FirstPrompt = source.Interface.FirstPrompt,
        Roles = source.Roles,
        Voices = source.Voices,
        PostIdeas = source.PostIdeas,
        DemoPosts = source.DemoPosts,
        Rules = source.Rules.Select(rule => new WorldRuleResponse(rule.Text, rule.Locked)).ToArray(),
        Stars = source.Stars.Select(star => new WorldStarResponse(
            star.Id.Value, star.Name, star.Role, Convert.ToInt32(star.Trait), star.About, star.Voice, star.AvatarSeed, star.Relation, star.AppearanceProfile, star.AgeBand)).ToArray(),
        Name = source.Player.Name,
        Role = source.Player.Role,
        Avatar = source.Player.Avatar,
        Voice = source.Player.Voice,
        Drama = MapDrama(source.Player.Drama),
        Subscribers = source.Player.Subscribers,
        Wants = source.Player.Wants.Select(want => new WantResponse(want.Id, want.Text, want.Reward, want.Done)).ToArray(),
        Achievements = source.Player.Achievements.Select(item => new CollectionCardResponse(item.Id, item.Emoji, item.Title, item.IsOpen)).ToArray(),
    };

    public World MapWorldDefinition(CreateWorldRequest source) => new()
    {
        Id = Domain.Worlds.WorldId.New(),
        BalanceKey = source.BalanceKey,
        Roles = source.Roles,
        Voices = source.Voices,
        PostIdeas = source.PostIdeas,
        DemoPosts = source.DemoPosts,
        Rules = [.. source.Rules.Where(rule => rule.Text != "Без тем 18+ и травли по внешности")
            .Select(rule => new WorldRule(rule.Text, false)), new WorldRule("Без тем 18+ и травли по внешности", true)],
        Stars = source.Stars.Select(star => new WorldStar(
                    StarId.From(star.Id),
                    star.Name,
                    star.Role,
                    MapStarTrait(star.Trait),
                    star.About,
                    star.Voice,
                    star.AvatarSeed,
                    0,
                    0,
                    DateTimeOffset.UnixEpoch,
                    star.AppearanceProfile,
                    star.AgeBand)).ToArray(),
        Interface = new()
        {
            Title = source.Title,
            Description = source.Description,
            AvatarStyle = source.AvatarStyle,
            Where = source.Where,
            FirstContext = source.FirstContext,
            FirstPrompt = source.FirstPrompt,
            Emoji = source.Emoji,
            Summary = source.Summary,
            Difficulty = MapWorldDifficulty(source.Difficulty),
            UnlockSkill = source.UnlockSkill,
            UnlockLevel = source.UnlockLevel,
        },
        Player = new()
        {
            PlayerId = PlayerId.From("00000000000000000000000000000000"),
            Name = source.Name,
            Avatar = source.Avatar,
            Role = source.Role,
            Voice = source.Voice,
            Drama = MapDrama(source.Drama),
            Wants = source.Wants.Select(want => new DailyWant(want.Id, want.Text, want.Reward, false)).ToArray(),
            Achievements = source.Achievements.Select(item => new AchievementProgress(item.Id, item.Emoji, item.Title, false)).ToArray(),
        },
    };

    public IReadOnlyDictionary<string, string> SanitizeProperties(
        IReadOnlyDictionary<string, JsonElement>? properties)
    {
        if (properties is null)
        {
            return new Dictionary<string, string>();
        }

        return properties
            .Where(item => item.Key.Length <= 64)
            .ToDictionary(item => item.Key, item => SanitizeElement(item.Value));
    }

    private static WorldDifficulty MapWorldDifficulty(int source) => source switch
    {
        1 => WorldDifficulty.Easy,
        2 => WorldDifficulty.Normal,
        3 => WorldDifficulty.Hard,
        _ => throw new ArgumentOutOfRangeException(nameof(source)),
    };

    private static StarTrait MapStarTrait(int source) =>
        Enum.Parse<StarTrait>(source.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static DirectMessageAuthor MapDirectMessageAuthor(string source)
    {
        return Enum.Parse<DirectMessageAuthor>(source, true);
    }

    private static GameEventKind MapGameEventKind(string source)
    {
        return Enum.Parse<GameEventKind>(source, true);
    }

    private static string MapEventId(DomainEventId source) => source.Value;

    private static string MapPlayerId(PlayerId source) => source.Value;

    private static string SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => SanitizeJsonString(element.GetString() ?? string.Empty, 64),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => $"kind:{element.ValueKind}",
        };
    }

    private static string SanitizeJsonString(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : $"len:{value.Length}";
    }

    [MapEnum(EnumMappingStrategy.ByName)]
    private partial WorldDrama MapDrama(Drama source);

    [MapEnum(EnumMappingStrategy.ByName)]
    private partial Drama MapDrama(WorldDrama source);

    private partial DirectMessageResponse MapDirectMessage(DirectMessage source);

    private partial WorldRuleResponse MapWorldRule(WorldRule source);

    private partial WorldRule MapWorldRule(WorldRuleResponse source);
}