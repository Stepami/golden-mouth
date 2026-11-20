using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses.Reply;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.AddReplyCommand;

internal sealed class AddReplyStateComparer(IPostRepository posts) : IStateComparer<AddReply, ReplyResponse>
{
    public async ValueTask<Func<ReplyResponse, CancellationToken, ValueTask>> CaptureAsync(
        AddReply command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        Func<ReplyResponse, CancellationToken, ValueTask> skip = static (_, _) => ValueTask.CompletedTask;
        var challenge = player.DailyChallenge;
        var worldId = command.WorldId;
        if (!challenge.Values.TryGetValue(worldId, out var tasks))
        {
            return skip;
        }

        var playerAuthor = player.Id.ToAuthorId();
        var isPlayer = command.AuthorId == playerAuthor;
        var starId = tasks.GetValueOrDefault(DailyTaskType.StarComment)?.StarId;
        if (isPlayer ? !tasks.ContainsKey(DailyTaskType.Comments) : starId?.ToAuthorId() != command.AuthorId)
        {
            return skip;
        }

        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        if (post is null || post.WorldId != worldId)
        {
            return skip;
        }

        var day = challenge.Day;
        if (!isPlayer && (post.RepostId is not null || post.AuthorId != playerAuthor ||
                          DateOnly.FromDateTime(post.CreatedAtUtc.UtcDateTime) != day))
        {
            return skip;
        }

        var commentCount = post.Comments.Count;
        var authorId = command.AuthorId;
        var type = isPlayer ? DailyTaskType.Comments : DailyTaskType.StarComment;
        return (response, _) =>
        {
            if (response.AuthorId == authorId && response.Post.Comments.Count > commentCount)
            {
                player.ApplyDailyChallengeProgress(worldId, new Dictionary<DailyTaskType, long> { [type] = 1 }, starId);
            }

            return ValueTask.CompletedTask;
        };
    }
}
