using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using Reki.Application.Auth;
using Reki.Application.Player;
using Reki.Domain.Player;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Infrastructure.Auth;

internal sealed class AuthSessionService(
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations,
    IOpenIddictTokenManager tokens,
    IOptions<AuthOptions> options,
    IPlayerRepository players) : IAuthSessionService
{
    public async Task<bool> LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var token = await tokens.FindByReferenceIdAsync(refreshToken, cancellationToken);
        if (token is null || !await tokens.HasTypeAsync(token, TokenTypeIdentifiers.RefreshToken, cancellationToken))
        {
            return false;
        }

        var authorizationId = await tokens.GetAuthorizationIdAsync(token, cancellationToken);
        if (authorizationId is not null &&
            await authorizations.FindByIdAsync(authorizationId, cancellationToken) is { } authorization)
        {
            await authorizations.TryRevokeAsync(authorization, cancellationToken);
        }

        await tokens.TryRevokeAsync(token, cancellationToken);
        return true;
    }

    public async Task<ClaimsPrincipal?> IssueAsync(
        ExternalUserId externalUserId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var application = await applications.FindByClientIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException("OpenIddict application is missing.");
        var player = await players.GetPlayerAsync(externalUserId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        var subject = player.Id.ToString();
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
        identity.SetClaim(Claims.Subject, subject);
        identity.SetDestinations(_ => []);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(Scopes.OfflineAccess);
        principal.SetResources(options.Value.Audience);

        var applicationId = await applications.GetIdAsync(application, cancellationToken)
            ?? throw new InvalidOperationException("OpenIddict application ID is missing.");
        var authorization = await authorizations.CreateAsync(
            identity,
            subject,
            applicationId,
            AuthorizationTypes.Permanent,
            principal.GetScopes(),
            cancellationToken);
        principal.SetAuthorizationId(await authorizations.GetIdAsync(authorization, cancellationToken));
        return principal;
    }
}
