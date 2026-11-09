using System.Security.Claims;
using Reki.Domain.Player;

namespace Reki.Application.Auth;

public interface IAuthSessionService
{
    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<ClaimsPrincipal?> IssueAsync(
        ExternalUserId externalUserId,
        string clientId,
        CancellationToken cancellationToken);
}