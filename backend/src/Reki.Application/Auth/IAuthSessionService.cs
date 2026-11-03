using System.Security.Claims;

namespace Reki.Application.Auth;

public interface IAuthSessionService
{
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<ClaimsPrincipal?> IssueAsync(
        IExternalUserIdProvider externalUserIdProvider,
        string clientId,
        CancellationToken cancellationToken);
}