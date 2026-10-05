using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

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

