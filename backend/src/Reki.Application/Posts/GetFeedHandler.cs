using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetFeedHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetFeedQuery, FeedView>
{
    public async ValueTask<FeedView> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        IReadOnlyList<GamePost> posts = await store.GetRecentPostsAsync(
            player.Id,
            20,
            cancellationToken);
        return new FeedView(player, posts);
    }
}

