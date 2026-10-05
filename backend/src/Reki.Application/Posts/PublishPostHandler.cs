using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class PublishPostHandler(
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<PublishPostCommand, PublishedPost>
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

        long subscribers = Math.Max(1, player.Subscribers);
        double scaledSubscribers = Math.Pow(subscribers, 0.6);
        long calculatedDelta = (long)Math.Round(scaledSubscribers * 1.1);
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
            "Соня",
            "sonya",
            "@sonya",
            "Есть голос. Продолжай в том же духе.",
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
        const string Hook = "Соня обещала вернуться к этой теме через пару постов";

        await store.AddPostAsync(post, cancellationToken);
        await store.AppendThreadAsync(player.Id, postId, Hook, now, cancellationToken);
        return new PublishedPost(post, Hook, "Соня");
    }
}

