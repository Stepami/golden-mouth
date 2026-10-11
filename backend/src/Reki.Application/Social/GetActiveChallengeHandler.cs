using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class GetActiveChallengeHandler(
    ITelegramUserAccessor telegramUser,
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<GetActiveChallengeQuery, ChallengeView?>
{
    public async ValueTask<ChallengeView?> Handle(
        GetActiveChallengeQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        return await challengeLifecycle.GetActiveViewAsync(player.Id, cancellationToken);
    }
}

