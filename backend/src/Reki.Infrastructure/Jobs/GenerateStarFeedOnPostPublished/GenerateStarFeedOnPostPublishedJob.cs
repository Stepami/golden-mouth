using Reki.Application.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.StarFeed;
using TickerQ.Exceptions;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedOnPostPublished;

internal sealed class GenerateStarFeedOnPostPublishedJob(IPostRepository postRepository, StarFeedPostGenerator generator) : ITickerFunction<StarFeedOnPostPublishedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedOnPostPublishedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var source = await postRepository.GetPostAsync(request.SourcePostId, request.WorldId, cancellationToken)
            ?? throw new TerminateExecutionException("The feed source is unavailable.");
        if (source.AuthorId != request.PlayerId.ToAuthorId())
        {
            throw new TerminateExecutionException("The feed source is invalid.");
        }

        await generator.GenerateAsync(
            context.Id,
            new StarFeedRequest(request.PlayerId, request.WorldId, request.PostId, request.CreatedAtUnixMilliseconds),
            StarFeedEvent.From(StarFeedEventType.PostPublished),
            source,
            null,
            [],
            cancellationToken);
    }
}
