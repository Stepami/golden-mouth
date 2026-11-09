using System.Diagnostics.CodeAnalysis;
using Reki.Application.Auth;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Infrastructure.Telegram.Auth;

internal sealed class TgMiniAppUserIdProvider(
    ITelegramUserAccessor telegramUserProvider) :
    IExternalUserIdProvider
{
    public bool TryGetExternalUserId([NotNullWhen(true)] out ExternalUserId? externalUserId)
    {
        externalUserId = ExternalUserId.Telegram(telegramUserProvider.User.Id);
        return true;
    }
}