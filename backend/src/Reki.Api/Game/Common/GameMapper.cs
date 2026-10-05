using System.Globalization;
using System.Text.Json;
using Reki.Application.Game;
using Reki.Contracts.Game;
using Reki.Domain.Game;

namespace Reki.Api.Game;

public sealed class GameMapper
{
    public PlayerResponse MapPlayer(PlayerView source)
    {
        PlayerProfile player = source.Player;
        IReadOnlyDictionary<SkillId, SkillResponse> skills = player.Skills.ToDictionary(
            item => Enum.Parse<SkillId>(item.Key, true),
            item => new SkillResponse(item.Value.Level, item.Value.Progress));
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
                .Select(message => new DirectMessageResponse(
                    Enum.Parse<DirectMessageAuthor>(message.From, true),
                    message.Text))
                .ToArray(),
            ["спросить про новый пост", "обсудить событие дня"],
            view.RepliesLeft);
    }

    public GameEventResponse MapEvent(DailyGameEvent source)
    {
        return new GameEventResponse(
            source.Id,
            Enum.Parse<GameEventKind>(source.Kind, true),
            source.Badge,
            source.Title,
            source.Text,
            source.Ideas,
            source.Reward,
            source.Note);
    }

    public WorldRulesResponse MapWorldRules(WorldRules source)
    {
        return new WorldRulesResponse(
            source.Title,
            source.Presets,
            source.Description,
            source.Rules.Select(rule => new WorldRuleResponse(rule.Text, rule.Locked)).ToArray());
    }

    public WorldRules MapWorldRulesRequest(UpdateWorldRulesRequest source)
    {
        return new WorldRules(
            string.Empty,
            "own",
            source.Title,
            source.Presets,
            source.Description,
            source.Rules.Select(rule => new WorldRule(rule.Text, rule.Locked)).ToArray());
    }

    public AnalyticsBatch MapAnalytics(AnalyticsBatchRequest source)
    {
        return new AnalyticsBatch(
            string.Empty,
            source.Session,
            source.Platform,
            source.Experiments.ToDictionary(
                item => item.Key,
                item => SanitizeString(item.Value)),
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
            [new WhyResponse("🔥", "в голосе канала", $"+{post.SubscriberDelta}")],
            post.Viral ? [new RibbonResponse("🚀", "залёт", null)] : [],
            bestComment is null ? null : new BestCommentResponse(bestComment.Avatar, bestComment.Text),
            post.Viral,
            post.Flop,
            post.Views,
            source.Hook);
    }

    public CommentResponse MapComment(GameComment source)
    {
        return new CommentResponse(
            source.Id,
            source.Author,
            source.Avatar,
            source.Handle,
            source.Text,
            source.Star,
            source.Me,
            source.Hidden);
    }

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

    private static string SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => SanitizeString(element.GetString() ?? string.Empty),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => string.Empty,
            _ => $"kind:{element.ValueKind}",
        };
    }

    private static string SanitizeString(string value)
    {
        return value.Length <= 64 ? value : $"len:{value.Length}";
    }
}
