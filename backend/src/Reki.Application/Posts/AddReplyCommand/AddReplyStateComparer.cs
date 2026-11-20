using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.RekiContext;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.AddReplyCommand;

internal sealed class AddReplyStateComparer(IPostRepository posts, DailyProgress progress) : IStateComparer<AddReply, ReplyResponse>
{
    public async ValueTask<Func<ReplyResponse, CancellationToken, ValueTask>?> CaptureAsync(
        AddReply command,
        IRekiContext context,
        PlayerChallenge challenge,
        CancellationToken cancellationToken)
    {
        var worldId = command.WorldId;
        if (!challenge.Values.TryGetValue(worldId, out var tasks))
        {
            return null;
        }

        var playerId = context.PlayerId;
        var playerAuthor = playerId.ToAuthorId();
        var isPlayer = command.AuthorId == playerAuthor;
        var starId = tasks.GetValueOrDefault(DailyTaskType.StarComment)?.StarId;
        if (isPlayer ? !tasks.ContainsKey(DailyTaskType.Comments) : starId?.ToAuthorId() != command.AuthorId)
        {
            return null;
        }

        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        if (post is null || post.WorldId != worldId)
        {
            return null;
        }

        var day = challenge.Day;
        if (!isPlayer && (post.RepostId is not null || post.AuthorId != playerAuthor ||
                          DateOnly.FromDateTime(post.CreatedAtUtc.UtcDateTime) != day))
        {
            return null;
        }

        var commentCount = post.Comments.Count;
        var authorId = command.AuthorId;
        var type = isPlayer ? DailyTaskType.Comments : DailyTaskType.StarComment;
        return (response, token) =>
            response.AuthorId == authorId && response.Post.Comments.Count > commentCount
                ? progress.ApplyAsync(playerId, day, worldId, new Dictionary<DailyTaskType, long> { [type] = 1 }, token, starId)
                : ValueTask.CompletedTask;
    }
}
