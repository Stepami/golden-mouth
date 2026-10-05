using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    PlayerSafetyGuard safetyGuard,
    StarTurnGenerator turnGenerator) : IRequestHandler<PublishPostCommand, PublishedPost>
{
    public async ValueTask<PublishedPost> Handle(
        PublishPostCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        player = gameDay.Normalize(player);
        int cap = player.GetPostsCap(balance.FreePostsPerDay, balance.PlusPostsPerDay);

        if (player.PostsUsed >= cap)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        await safetyGuard.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        WorldRules? storedRules = await store.GetWorldRulesAsync(
            player.Id,
            player.World,
            cancellationToken);
        WorldRules worldRules = storedRules ?? WorldRulesDefaults.Create(player.Id, player.World);
        IReadOnlyList<GamePost> recentPosts = await store.GetRecentPostsAsync(
            player.Id,
            3,
            cancellationToken);
        bool hateEnabled = player.Settings.Drama != "off" &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= now);
        GeneratedPostTurn turn = await turnGenerator.GeneratePostTurnAsync(
            new StarPostContext(
                request.Text,
                player.World,
                string.Join("; ", worldRules.Rules.Select(rule => rule.Text)),
                player.Settings.Voice,
                GetStage(player.Subscribers),
                recentPosts.Select(post => post.Text).ToArray(),
                hateEnabled),
            cancellationToken);

        long subscribers = Math.Max(1, player.Subscribers);
        double scaledSubscribers = Math.Pow(subscribers, 0.6);
        double quality = (turn.Brave + turn.Humor + turn.Topic) / 3;
        long calculatedDelta = (long)Math.Round(scaledSubscribers * (0.5 + quality));
        long delta = Math.Max(1, calculatedDelta);
        bool viral = player.PostsUsed == 2;
        if (viral)
        {
            delta *= 3;
        }

        PlayerProfile updated = player.ConsumePost(delta);
        bool replaced = await store.TryReplacePlayerAsync(
            updated,
            player.Version,
            cancellationToken);
        if (!replaced)
        {
            throw new RequestConflictException("The player state changed while publishing the post.");
        }

        string postId = Guid.NewGuid().ToString("N");
        string commentId = Guid.NewGuid().ToString("N");
        var starComment = new GameComment(
            commentId,
            turn.StarName,
            turn.StarId,
            $"@{turn.StarId}",
            turn.Comment,
            true,
            false,
            false);
        long views = Math.Max(48, updated.Subscribers + delta);
        var post = new GamePost(
            postId,
            player.Id,
            request.Text,
            views,
            [starComment],
            now,
            delta,
            viral,
            false);
        string hook = turn.Hook;

        await store.AddPostAsync(post, cancellationToken);
        await store.AppendThreadAsync(player.Id, postId, hook, now, cancellationToken);
        return new PublishedPost(post, hook, turn.StarName, turn.Reaction, turn.Reasons);
    }

    private static string GetStage(long subscribers)
    {
        return subscribers switch
        {
            < 1000 => "mask",
            < 5000 => "views",
            < 10000 => "strike",
            _ => "new_line",
        };
    }
}