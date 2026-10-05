using System.Globalization;
using System.Text.Json;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Social;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Mapping;

[Mapper]
public sealed partial class ApiMapper
{
    public PlayerResponse MapPlayer(PlayerView source)
    {
        PlayerProfile player = source.Player;
        IReadOnlyDictionary<SkillId, SkillResponse> skills = player.Skills.ToDictionary(
            item => Enum.Parse<SkillId>(item.Key, true),
            item => MapSkill(item.Value));
        var week = Enumerable.Range(0, 7)
            .Select(day => day < Math.Min(7, player.StreakDays))
            .ToArray();

        return new PlayerResponse(
            player.Id,
            player.Name,
            player.Handle,
            player.Role,
            player.Avatar,
            Enum.Parse<Address>(player.Address, true),
            Enum.Parse<WorldId>(player.World, true),
            player.Subscribers,
            source.PostsLeft,
            source.PostsCap,
            new StreakResponse(player.StreakDays, week, "ещё день — и бонус"),
            skills,
            [],
            [],
            string.Empty,
            "свой голос",
            null,
            new PlayerSettingsResponse(
                Enum.Parse<Drama>(player.Settings.Drama, true),
                player.Settings.Notifications,
                player.Settings.Quiet,
                player.Settings.Voice,
                player.Settings.Plus),
            new Dictionary<string, string>());
    }

    public FeedItemResponse MapFeedItem(GamePost post)
    {
        return new FeedItemResponse(
            FeedItemKind.Post,
            null,
            null,
            null,
            post.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            post.Text,
            [new ReactionResponse("🔥", post.Viral ? 3 : 1)],
            null);
    }

    public DmResponse MapDirectMessages(DirectMessageView view, string starId)
    {
        StarResponse star = CreateStar(starId);
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

    public AnalyticsBatch MapAnalytics(AnalyticsBatchRequest source)
    {
        return new AnalyticsBatch(
            string.Empty,
            source.Session,
            source.Platform,
            source.Experiments.ToDictionary(
                item => item.Key,
                item => SanitizeJsonString(item.Value, 64)),
            source.Events.Select(item => new AnalyticsEntry(
                item.Name,
                item.Timestamp,
                item.Screen,
                item.Prev,
                SanitizeProperties(item.Props))).ToArray(),
            default);
    }

    public PostResultResponse MapPublishedPost(PublishedPost source)
    {
        GamePost post = source.Post;
        GameComment? bestComment = post.Comments.FirstOrDefault();
        return new PostResultResponse(
            post.Id,
            post.SubscriberDelta,
            post.Viral ? "Пост залетел" : "Канал растёт",
            source.Reasons
                .Select(reason => new WhyResponse(reason.Icon, reason.Label, $"+{post.SubscriberDelta}"))
                .ToArray(),
            post.Viral ? [new RibbonResponse("🚀", "залёт", null)] : [],
            bestComment is null ? null : new BestCommentResponse(bestComment.Avatar, bestComment.Text),
            post.Viral,
            post.Flop,
            post.Views,
            source.Hook);
    }

    public partial CommentResponse MapComment(GameComment source);

    public partial SendDirectMessageResponse MapSentDirectMessage(SentDirectMessage source);

    [MapProperty(nameof(FriendSummary.Subscribers), nameof(FriendResponse.Subs))]
    public partial FriendResponse MapFriend(FriendSummary source);

    public partial IReadOnlyList<FriendResponse> MapFriends(IReadOnlyList<FriendSummary> source);

    [MapperIgnoreSource(nameof(DailyGameEvent.PlayerId))]
    [MapperIgnoreSource(nameof(DailyGameEvent.DayStartedAtUtc))]
    [MapperIgnoreSource(nameof(DailyGameEvent.Answer))]
    public partial GameEventResponse MapEvent(DailyGameEvent source);

    [MapperIgnoreSource(nameof(WorldRules.PlayerId))]
    [MapperIgnoreSource(nameof(WorldRules.WorldId))]
    public partial WorldRulesResponse MapWorldRules(WorldRules source);

    [MapValue(nameof(WorldRules.PlayerId), "")]
    [MapValue(nameof(WorldRules.WorldId), "own")]
    public partial WorldRules MapWorldRulesRequest(UpdateWorldRulesRequest source);

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

    private static StarResponse CreateStar(string starId)
    {
        return new StarResponse(
            starId,
            starId.Equals("mira", StringComparison.OrdinalIgnoreCase) ? "Мира" : "Соня",
            "звезда мира",
            "слышит свой голос",
            "Реагирует на развитие канала.",
            0);
    }

    private static DirectMessageAuthor MapDirectMessageAuthor(string source)
    {
        return Enum.Parse<DirectMessageAuthor>(source, true);
    }

    private static GameEventKind MapGameEventKind(string source)
    {
        return Enum.Parse<GameEventKind>(source, true);
    }

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

    private partial SkillResponse MapSkill(SkillProgress source);

    private partial DirectMessageResponse MapDirectMessage(DirectMessage source);

    private partial WorldRuleResponse MapWorldRule(WorldRule source);

    private partial WorldRule MapWorldRule(WorldRuleResponse source);
}
