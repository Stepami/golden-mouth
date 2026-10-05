using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Social;

public sealed class GetFriendsHandler(PlayerReader playerReader)
    : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        _ = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        return [];
    }
}

