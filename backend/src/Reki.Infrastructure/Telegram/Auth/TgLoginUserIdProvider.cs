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
    public Task<ExternalUserId?> GetExternalUserIdOrNullAsync(CancellationToken cancellationToken)
    {
        var id = httpContextAccessor.HttpContext?.User.FindFirstValue("id");
        var parsed = long.TryParse(
            id,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var telegramId);
        return Task.FromResult(parsed && telegramId > 0 ? (ExternalUserId?)ExternalUserId.Telegram(telegramId) : null);
    }
}
