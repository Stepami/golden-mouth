using Reki.Application.Auth;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Infrastructure.Telegram.Auth;

internal sealed class TgMiniAppUserIdProvider(
    ITelegramUserAccessor telegramUserProvider) :
    IExternalUserIdProvider
{
    public Task<ExternalUserId?> GetExternalUserIdOrNullAsync(CancellationToken cancellationToken) =>
        Task.FromResult<ExternalUserId?>(ExternalUserId.Telegram(telegramUserProvider.User.Id));
}
