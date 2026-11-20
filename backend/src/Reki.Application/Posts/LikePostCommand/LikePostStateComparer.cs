using Mediator;
using Reki.Application.Player.Challenges;
using Reki.Application.RekiContext;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostStateComparer(IPostRepository posts, DailyProgress progress) : IStateComparer<LikePost, Unit>
{
    public async ValueTask<Func<Unit, CancellationToken, ValueTask>?> CaptureAsync(
        LikePost command,
        IRekiContext context,
        PlayerChallenge challenge,
        CancellationToken cancellationToken)
    {
        var playerId = context.PlayerId;
        var authorId = command.AuthorId;
        if (authorId != playerId.ToAuthorId() || !challenge.Completion.ContainsKey(DailyTaskType.Likes))
        {
            return null;
        }

        var postId = command.PostId;
        var post = await posts.GetPostAsync(postId, cancellationToken);
        if (post is null || post.LikedBy.Contains(authorId) ||
            !challenge.Values.TryGetValue(post.WorldId, out var tasks) || !tasks.ContainsKey(DailyTaskType.Likes))
        {
            return null;
        }

        var worldId = post.WorldId;
        var day = challenge.Day;
        return async (_, token) =>
        {
            var after = await posts.GetPostAsync(postId, token);
            if (after is not null && after.LikedBy.Contains(authorId))
            {
                await progress.ApplyAsync(playerId, day, worldId, new Dictionary<DailyTaskType, long> { [DailyTaskType.Likes] = 1 }, token);
            }
        };
    }
}
