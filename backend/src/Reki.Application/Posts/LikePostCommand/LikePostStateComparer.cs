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
        var challenge = player.DailyChallenge;
        var authorId = command.AuthorId;
        if (authorId != player.Id.ToAuthorId())
        {
            return skip;
        }

        if (!challenge.Completion.ContainsKey(DailyTaskType.Likes))
        {
            return skip;
        }

        var postId = command.PostId;
        var post = await posts.GetPostAsync(postId, cancellationToken);
        if (post is null || post.LikedBy.Contains(authorId))
        {
            return skip;
        }

        if (!challenge.Values.TryGetValue(post.WorldId, out var tasks) || !tasks.ContainsKey(DailyTaskType.Likes))
        {
            return skip;
        }

        return new LikeContinuation(posts, postId, post.WorldId, authorId);
    }

    private sealed class LikeContinuation(
        IPostRepository posts,
        PostId postId,
        WorldId worldId,
        AuthorId authorId) : IStateComparer<LikePost, Unit>.IContinuation
    {
        public async ValueTask ApplyAsync(Unit response, PlayerProfile player, CancellationToken cancellationToken)
        {
            var after = await posts.GetPostAsync(postId, cancellationToken);
            if (after is not null && after.LikedBy.Contains(authorId))
            {
                player.ApplyDailyChallengeProgress(
                    worldId,
                    new Dictionary<DailyTaskType, long> { [DailyTaskType.Likes] = 1 });
            }
        }
    }
}
