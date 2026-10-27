using Reki.Application.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.StarFeed;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedOnCommentPublished;

internal sealed class GenerateStarFeedOnCommentPublishedJob(IPostRepository postRepository, StarFeedPostGenerator generator) : ITickerFunction<StarFeedOnCommentPublishedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedOnCommentPublishedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var source = await postRepository.GetPostAsync(request.SourcePostId, request.WorldId, cancellationToken)
            ?? throw new TerminateExecutionException("The feed source is unavailable.");
        var comment = source.Comments.FirstOrDefault(item => item.Id == request.SourceCommentId);
        if (comment is null || comment.Hidden || comment.AuthorId != request.PlayerId.ToAuthorId())
        {
            throw new TerminateExecutionException("The feed comment is unavailable.");
        }

        var branch = source.GetBranch(comment.Id);
        if (branch.Any(item => item.Hidden))
        {
            throw new TerminateExecutionException("The feed branch is unavailable.");
        }

        await generator.GenerateAsync(
            context.Id,
            new StarFeedRequest(request.PlayerId, request.WorldId, request.PostId, request.CreatedAtUnixMilliseconds),
            StarFeedEvent.From(StarFeedEventType.CommentPublished),
            source,
            comment,
            branch.TakeLast(8).ToArray(),
            cancellationToken);
    }
}
