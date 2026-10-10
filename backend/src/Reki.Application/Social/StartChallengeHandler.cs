using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Social;

public sealed class StartChallengeHandler(
    ITelegramUserAccessor telegramUser,
    PlayerReader playerReader,
    ChallengeLifecycle challengeLifecycle)
    : IRequestHandler<StartChallengeCommand>
{
    public async ValueTask<Unit> Handle(
        StartChallengeCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        await challengeLifecycle.StartOrAcceptAsync(player, request.FriendId, cancellationToken);
        return Unit.Value;
    }
}

