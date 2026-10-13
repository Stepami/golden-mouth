using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemResponse>>
{
    private const int FeedSize = 20;

    public async ValueTask<IReadOnlyList<FeedItemResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var posts = await sender.Send(new GetChannelPostsQuery(world.Id, FeedSize), cancellationToken);
        return [.. posts.Select(post => PostContractMapper.MapFeedItem(post, world, true))];
    }
}