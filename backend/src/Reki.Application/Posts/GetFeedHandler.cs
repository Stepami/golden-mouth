using Mediator;
using Reki.Application.Player;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(
    ISender sender)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemResponse>>
{
    private const int FeedSize = 20;

    public async ValueTask<IReadOnlyList<FeedItemResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var posts = await sender.Send(new GetChannelPostsQuery(player.Id, FeedSize), cancellationToken);
        return posts
            .Select(post => PostContractMapper.MapFeedItem(post, player, true))
            .ToArray();
    }
}
