using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed class GetChannelPostsHandler(IGameStore store, ISender sender)
    : IRequestHandler<GetChannelPostsQuery, IReadOnlyList<GamePost>>
{
    public async ValueTask<IReadOnlyList<GamePost>> Handle(
        GetChannelPostsQuery request,
        CancellationToken cancellationToken)
    {
        var playerId = request.PlayerId ?? (await sender.Send(new GetPlayerQuery(), cancellationToken)).Id;
        return await store.GetPostsAsync(playerId, request.Limit, cancellationToken);
    }
}
