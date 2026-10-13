using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class GetFriendsHandler(
    IRekiContextAccessor contextAccessor)
    : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        _ = await rekiContext.Player;
        return [];
    }
}

