using Mediator;
using Reki.Application.Player.Challenges;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostStateComparer(IPostRepository posts) : IStateComparer<LikePost, Unit>
{
    public async ValueTask<IStateComparer<LikePost, Unit>.IContinuation> CaptureAsync(
        LikePost command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var skip = EmptyStateComparerContinuation<LikePost, Unit>.Instance;
        var (_, completion, values) = player.DailyChallenge;

        var playerMayLiked =
            command.AuthorId == player.Id.ToAuthorId() &&
            completion.ContainsKey(DailyTaskType.Likes);
        if (!playerMayLiked)
        {
            return skip;
        }

        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        if (post is null || post.LikedBy.Contains(command.AuthorId))
        {
            return skip;
        }

        var worldHasLikesTask =
            values.TryGetValue(post.WorldId, out var tasks) &&
            tasks.ContainsKey(DailyTaskType.Likes);
        if (!worldHasLikesTask)
        {
            return skip;
        }

        return new LikeContinuation(posts, command.PostId, post.WorldId, command.AuthorId);
    }

    private sealed class LikeContinuation(
        IPostRepository posts,
        PostId postId,
        WorldId worldId,
        AuthorId authorId) : IStateComparer<LikePost, Unit>.IContinuation
    {
        public async ValueTask<bool> ApplyAsync(
            Unit response,
            PlayerProfile player,
            CancellationToken cancellationToken)
        {
            var after = await posts.GetPostAsync(postId, cancellationToken);
            if (after is not null && after.LikedBy.Contains(authorId))
            {
                return player.ApplyDailyChallengeProgress(
                    worldId,
                    new Dictionary<DailyTaskType, long> { [DailyTaskType.Likes] = 1 });
            }

            return false;
        }
    }
}
