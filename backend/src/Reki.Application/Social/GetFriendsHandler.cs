using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class GetFriendsHandler(
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await rekiContextAccessor.Context.Player;
        return [];
    }
}

