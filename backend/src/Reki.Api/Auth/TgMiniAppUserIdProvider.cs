using System.Diagnostics.CodeAnalysis;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Auth;

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