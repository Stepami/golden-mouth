using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Reki.Application.Auth;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Telegram.Auth;

internal sealed class TgLoginUserIdProvider(
    IHttpContextAccessor httpContextAccessor) :
    IExternalUserIdProvider
{
    public bool TryGetExternalUserId([NotNullWhen(true)] out ExternalUserId? externalUserId)
    {
        var id = httpContextAccessor.HttpContext?.User.FindFirstValue("id");
        var parsed = long.TryParse(
            id,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var telegramId);

        if (!parsed || telegramId <= 0)
        {
            externalUserId = null;
            return false;
        }

        externalUserId = ExternalUserId.Telegram(telegramId);
        return true;
    }
}
