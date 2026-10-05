using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;
using Reki.Domain.Game.Posts;

namespace Reki.Application.Game.Posts;

public sealed class GetFeedHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<GetFeedQuery, IReadOnlyList<GamePost>>
{
    public async ValueTask<IReadOnlyList<GamePost>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        return await store.GetRecentPostsAsync(player.Id, 20, cancellationToken);
    }
}

