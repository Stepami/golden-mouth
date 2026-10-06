using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<FeedItemResponse>>
{
    public async ValueTask<IReadOnlyList<FeedItemResponse>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetRecentPostsAsync(
            player.Id,
            20,
            cancellationToken);
        return posts.Select(post => PostContractMapper.MapFeedItem(post, player)).ToArray();
    }
}

