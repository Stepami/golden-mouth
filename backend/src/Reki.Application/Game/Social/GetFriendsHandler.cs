using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

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

