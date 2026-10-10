using Mediator;
using Reki.Application.Common;
using Reki.Contracts.Player;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class GetReferralHandler(
    ITelegramUserAccessor telegramUser,
    PlayerReader playerReader) : IRequestHandler<GetReferralQuery, ReferralResponse>
{
    public async ValueTask<ReferralResponse> Handle(GetReferralQuery request, CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        return new ReferralResponse($"ref_{player.Id.Value}");
    }
}
