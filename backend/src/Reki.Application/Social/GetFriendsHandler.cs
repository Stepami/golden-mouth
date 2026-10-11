using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class GetFriendsHandler(
    ITelegramUserAccessor telegramUser,
    PlayerReader playerReader)
    : IRequestHandler<GetFriendsQuery, IReadOnlyList<FriendSummary>>
{
    public async ValueTask<IReadOnlyList<FriendSummary>> Handle(
        GetFriendsQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        _ = await playerReader.GetAsync(telegramUserId, cancellationToken);
        return [];
    }
}

