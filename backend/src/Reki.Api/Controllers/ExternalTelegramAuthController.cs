using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using Reki.Infrastructure.Auth;
using TgMiniAppAuth.AuthContext.User;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations,
    Microsoft.Extensions.Options.IOptions<AuthOptions> options) : ControllerBase
{
    [HttpPost("miniapp", Name = "ExchangeMiniApp")]
    [Authorize(AuthenticationSchemes = "TMiniApp", Policy = "TMiniApp")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExchangeMiniAppAsync(
        [FromForm] TokenExchangeRequest request,
        [FromServices] ITelegramUserAccessor telegramUser,
        CancellationToken cancellationToken)
    {
        if (request.GrantType != AuthConstants.MiniAppGrant)
        {
            return InvalidGrant();
        }

        return await IssueAsync(telegramUser.User.Id, cancellationToken);
    }

    [HttpPost("widget", Name = "ExchangeTelegramLogin")]
    [Authorize(AuthenticationSchemes = AuthConstants.TelegramLoginScheme)]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.GrantType != AuthConstants.TelegramLoginGrant ||
            !long.TryParse(User.FindFirstValue("id"), NumberStyles.None, CultureInfo.InvariantCulture, out var telegramId) ||
            telegramId <= 0)
        {
            return InvalidGrant();
        }

        return await IssueAsync(telegramId, cancellationToken);
    }

    private async Task<IActionResult> IssueAsync(long telegramId, CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request is missing.");
        var application = await applications.FindByClientIdAsync(
            request.ClientId ?? string.Empty,
            cancellationToken);
        if (application is null)
        {
            return InvalidGrant();
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

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private ForbidResult InvalidGrant() => Forbid(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
