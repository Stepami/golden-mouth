using System.Security.Claims;
using Reki.Domain.Player;

namespace Reki.Application.Auth;

public interface IAuthSessionService
{
    Task DeletePlayerSessionsAsync(PlayerId playerId, CancellationToken cancellationToken);

    Task<bool> LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task<IssueResult> IssueAsync(
        IExternalUserIdProvider provider,
        string clientId,
        CancellationToken cancellationToken);
}

public sealed record IssueResult(bool Ok, string? Error, ClaimsPrincipal? Identity);
