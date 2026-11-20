using Mediator;
using Reki.Application.Player.Challenges;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostStateComparer(IPostRepository posts) : IStateComparer<LikePost, Unit>
{
    public async ValueTask<Func<Unit, CancellationToken, ValueTask>> CaptureAsync(
        LikePost command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        Func<Unit, CancellationToken, ValueTask> skip = static (_, _) => ValueTask.CompletedTask;
        var challenge = player.DailyChallenge;
        var authorId = command.AuthorId;
        if (authorId != player.Id.ToAuthorId() || !challenge.Completion.ContainsKey(DailyTaskType.Likes))
        {
            return skip;
        }

        var postId = command.PostId;
        var post = await posts.GetPostAsync(postId, cancellationToken);
        if (post is null || post.LikedBy.Contains(authorId) ||
            !challenge.Values.TryGetValue(post.WorldId, out var tasks) || !tasks.ContainsKey(DailyTaskType.Likes))
        {
            return skip;
        }

        var worldId = post.WorldId;
        return async (_, token) =>
        {
            var after = await posts.GetPostAsync(postId, token);
            if (after is not null && after.LikedBy.Contains(authorId))
            {
                player.ApplyDailyChallengeProgress(worldId, new Dictionary<DailyTaskType, long> { [DailyTaskType.Likes] = 1 });
            }
        };
    }
}
