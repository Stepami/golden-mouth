using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Social;

public sealed class GetFriendsHandler(
    ISender sender)
    : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await sender.Send(new GetPlayerQuery(), cancellationToken);
        return [];
    }
}

