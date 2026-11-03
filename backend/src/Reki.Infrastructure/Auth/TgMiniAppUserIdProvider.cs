using System.Diagnostics.CodeAnalysis;
using Reki.Application.Auth;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Infrastructure.Auth;

internal sealed class TgMiniAppUserIdProvider(
    ITelegramUserAccessor telegramUserProvider) :
    IExternalUserIdProvider
{
    public bool TryGetExternalUserId([NotNullWhen(true)] out long? externalUserId)
    {
        externalUserId = telegramUserProvider.User.Id;
        return true;
    }
}