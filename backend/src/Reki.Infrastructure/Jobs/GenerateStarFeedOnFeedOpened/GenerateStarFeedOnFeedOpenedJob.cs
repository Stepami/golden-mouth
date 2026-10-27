using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.StarFeed;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedOnFeedOpened;

internal sealed class GenerateStarFeedOnFeedOpenedJob(StarFeedPostGenerator generator) : ITickerFunction<StarFeedRequest>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<StarFeedRequest> context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        await generator.GenerateAsync(
            context.Id,
            request,
            StarFeedEvent.From(StarFeedEventType.FeedOpened),
            null,
            null,
            [],
            cancellationToken);
    }
}
