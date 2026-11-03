using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using Reki.Infrastructure.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth")]
public sealed class AuthController(AuthSessionService sessions) : ControllerBase
{
    [Authorize(Policy = AuthConstants.Policies.RekiRefreshGrant)]
    [HttpPost("refresh", Name = "RefreshAuth")]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> RefreshAsync(
        [FromForm] RefreshTokenRequest form,
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return !result.Succeeded || result.Principal is null
            ? InvalidGrant()
            : TypedResults.SignIn(result.Principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [AllowAnonymous]
    [HttpPost("logout", Name = "LogoutAuth")]
    public async Task<Results<NoContent, BadRequest>> LogoutAsync(
        [FromForm] LogoutRequest request,
        CancellationToken cancellationToken) =>
        await sessions.LogoutAsync(request, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.BadRequest();

    private static ForbidHttpResult InvalidGrant() => TypedResults.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
