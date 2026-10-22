using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class FriendsQueryHandler(
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<FriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        FriendsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await rekiContextAccessor.Context.Player;
        return [];
    }
}

