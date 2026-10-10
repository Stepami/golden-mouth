using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Player;

public sealed class ClaimPlusInterestHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameBalance balance,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<ClaimPlusInterestCommand, int>
{
    public async ValueTask<int> Handle(
        ClaimPlusInterestCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile storedPlayer = await playerReader.GetAsync(telegramUserId, cancellationToken);
        PlayerProfile player = gameDay.Normalize(storedPlayer);
        return await store.ClaimPlusInterestAsync(
            telegramUserId,
            balance.PlusInterestBonus,
            request.Interest,
            player.PostsPeriodStartedAtUtc == storedPlayer.PostsPeriodStartedAtUtc ? null : player,
            cancellationToken);
    }
}