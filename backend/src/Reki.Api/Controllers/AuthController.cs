using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth")]
public sealed class AuthController(
    IOpenIddictTokenManager tokens,
    IOpenIddictAuthorizationManager authorizations,
    Microsoft.Extensions.Options.IOptions<AuthOptions> options) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("refresh", Name = "RefreshAuth")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshAsync(
        [FromForm] RefreshTokenRequest form,
        CancellationToken cancellationToken)
    {
        if (form.GrantType != GrantTypes.RefreshToken)
        {
            return InvalidGrant();
        }

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return InvalidGrant();
        }

        return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpPost("logout", Name = "LogoutAuth")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAsync(
        [FromForm] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ClientId != options.Value.ClientId)
        {
            return BadRequest();
        }

        var token = await tokens.FindByReferenceIdAsync(request.RefreshToken, cancellationToken);
        if (token is null || !await tokens.HasTypeAsync(token, TokenTypeIdentifiers.RefreshToken, cancellationToken))
        {
            return BadRequest();
        }

        var authorizationId = await tokens.GetAuthorizationIdAsync(token, cancellationToken);
        if (authorizationId is not null &&
            await authorizations.FindByIdAsync(authorizationId, cancellationToken) is { } authorization)
        {
            await authorizations.TryRevokeAsync(authorization, cancellationToken);
        }

        await tokens.TryRevokeAsync(token, cancellationToken);
        return NoContent();
    }

    private ForbidResult InvalidGrant() => Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
