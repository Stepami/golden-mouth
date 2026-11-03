using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using TgMiniAppAuth.AuthContext.User;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(AuthSessionService sessions) : ControllerBase
{
    [HttpPost("miniapp", Name = "ExchangeMiniApp")]
    [Authorize(AuthenticationSchemes = "TMiniApp", Policy = "TMiniApp")]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeMiniAppAsync(
        [FromForm] TokenExchangeRequest request,
        [FromServices] ITelegramUserAccessor telegramUser,
        CancellationToken cancellationToken)
    {
        var principal = await sessions.ExchangeMiniAppAsync(telegramUser.User.Id, request, cancellationToken);
        return principal is null ? InvalidGrant() : TypedResults.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("widget", Name = "ExchangeTelegramLogin")]
    [Authorize(AuthenticationSchemes = AuthConstants.TelegramLoginScheme)]
    [ProducesResponseType<AuthTokenResponse>(StatusCodes.Status200OK)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        var principal = await sessions.ExchangeWidgetAsync(User, request, cancellationToken);
        return principal is null ? InvalidGrant() : TypedResults.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ForbidHttpResult InvalidGrant() => TypedResults.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
