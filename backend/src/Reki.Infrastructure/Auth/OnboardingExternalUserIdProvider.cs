using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Reki.Application.Auth;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Auth;

internal sealed class OnboardingExternalUserIdProvider(IHttpContextAccessor httpContextAccessor) : IExternalUserIdProvider
{
    public Task<ExternalUserId?> GetExternalUserIdOrNullAsync(CancellationToken cancellationToken)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(AuthConstants.Claims.ExternalUserId);
        return Task.FromResult(value is not null && ExternalUserId.TryFrom(value, out var id) ? (ExternalUserId?)id : null);
    }
}
