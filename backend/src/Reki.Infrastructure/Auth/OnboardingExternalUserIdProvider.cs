using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Reki.Application.Auth;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Auth;

internal sealed class OnboardingExternalUserIdProvider(IHttpContextAccessor httpContextAccessor) : IExternalUserIdProvider
{
    public bool TryGetExternalUserId([NotNullWhen(true)] out ExternalUserId? externalUserId)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthConstants.Claims.ExternalUserId);
        if (value is not null && ExternalUserId.TryFrom(value, out var id))
        {
            externalUserId = id;
            return true;
        }

        externalUserId = null;
        return false;
    }
}
