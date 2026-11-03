using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Requests.Auth;
using Reki.Infrastructure.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Auth;

public sealed class AuthSessionService(
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations,
    IOpenIddictTokenManager tokens,
    IOptions<AuthOptions> options)
{
    public static async Task<ClaimsPrincipal?> RefreshAsync(
        HttpContext context,
        RefreshTokenRequest request)
    {
        if (request.GrantType != GrantTypes.RefreshToken)
        {
            return null;
        }

        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return result.Succeeded ? result.Principal : null;
    }

    public Task<ClaimsPrincipal?> ExchangeMiniAppAsync(
        long telegramId,
        TokenExchangeRequest request,
        CancellationToken cancellationToken) =>
        request.GrantType == AuthConstants.MiniAppGrant
            ? IssueAsync(telegramId, request.ClientId, cancellationToken)
            : Task.FromResult<ClaimsPrincipal?>(null);

    public Task<ClaimsPrincipal?> ExchangeWidgetAsync(
        ClaimsPrincipal user,
        TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.GrantType != AuthConstants.TelegramLoginGrant ||
            !long.TryParse(user.FindFirstValue("id"), NumberStyles.None, CultureInfo.InvariantCulture, out var telegramId) ||
            telegramId <= 0)
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }

        return IssueAsync(telegramId, request.ClientId, cancellationToken);
    }

    public async Task<bool> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (request.ClientId != options.Value.ClientId)
        {
            return false;
        }

        var token = await tokens.FindByReferenceIdAsync(request.RefreshToken, cancellationToken);
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

    private async Task<ClaimsPrincipal?> IssueAsync(
        long telegramId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var application = await applications.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return null;
        }

        var subject = telegramId.ToString(CultureInfo.InvariantCulture);
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType);
        identity.SetClaim(Claims.Subject, subject);
        identity.SetClaim(AuthConstants.TelegramUserIdClaim, subject);
        identity.SetDestinations(claim => claim.Type == AuthConstants.TelegramUserIdClaim
            ? [Destinations.AccessToken] : []);

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
