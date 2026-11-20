using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses.Reply;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AddReplyCommand;

internal sealed class AddReplyStateComparer(IPostRepository posts) : IStateComparer<AddReply, ReplyResponse>
{
    public async ValueTask<IStateComparer<AddReply, ReplyResponse>.IContinuation> CaptureAsync(
        AddReply command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var skip = EmptyStateComparerContinuation<AddReply, ReplyResponse>.Instance;
        var (day, _, values) = player.DailyChallenge;

        if (!values.TryGetValue(command.WorldId, out var tasks))
        {
            return skip;
        }

        var type = command.AuthorId == player.Id.ToAuthorId()
            ? DailyTaskType.Comments
            : DailyTaskType.StarComment;
        if (!tasks.TryGetValue(type, out var task))
        {
            return skip;
        }

        var targetStarDidNotComment =
            type == DailyTaskType.StarComment &&
            task.StarId?.ToAuthorId() != command.AuthorId;
        if (targetStarDidNotComment)
        {
            return skip;
        }

        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        if (post is null || post.WorldId != command.WorldId)
        {
            return skip;
        }

        if (type == DailyTaskType.StarComment)
        {
            var isOwnOriginalPost =
                post.RepostId is null &&
                post.AuthorId == player.Id.ToAuthorId();
            if (!isOwnOriginalPost || DateOnly.FromDateTime(post.CreatedAtUtc.UtcDateTime) != day)
            {
                return skip;
            }
        }

        return new ReplyContinuation(command.WorldId, command.AuthorId, type, task.StarId, post.Comments.Count);
    }

    private sealed class ReplyContinuation(
        WorldId worldId,
        AuthorId authorId,
        DailyTaskType type,
        StarId? starId,
        int commentCount) : IStateComparer<AddReply, ReplyResponse>.IContinuation
    {
        public ValueTask<bool> ApplyAsync(
            ReplyResponse response,
            PlayerProfile player,
            CancellationToken cancellationToken)
        {
            if (response.AuthorId == authorId && response.Post.Comments.Count > commentCount)
            {
                var challengeChanged = player.ApplyDailyChallengeProgress(
                    worldId,
                    new Dictionary<DailyTaskType, long> { [type] = 1 },
                    starId);
                return ValueTask.FromResult(challengeChanged);
            }

            return ValueTask.FromResult(false);
        }
    }
}
