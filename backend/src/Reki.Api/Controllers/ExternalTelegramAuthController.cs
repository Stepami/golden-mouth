using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using Telegram.Bot;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(AuthSessionService sessions) : ControllerBase
{
    private const string Scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;

    [AllowAnonymous]
    [HttpGet("client-id", Name = "GetTelegramClientId")]
    public Ok<long> GetTelegramClientId(
        [FromServices] ITelegramBotClient bot) =>
        TypedResults.Ok(bot.BotId);

    [HttpPost("miniapp", Name = "ExchangeMiniApp")]
    [Authorize(AuthenticationSchemes = "TMiniApp", Policy = "TMiniApp")]
    [Authorize(Policy = AuthConstants.MiniAppGrantPolicy)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeMiniAppAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.MiniAppGrant)] IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var principal = await sessions.IssueAsync(
            externalUserIdProvider,
            request.ClientId,
            cancellationToken);
        return principal is null
            ? InvalidGrant()
            : TypedResults.SignIn(principal, authenticationScheme: Scheme);
    }

    [HttpPost("widget", Name = "ExchangeTelegramLogin")]
    [Authorize(AuthenticationSchemes = AuthConstants.TelegramLoginScheme)]
    [Authorize(Policy = AuthConstants.TelegramLoginGrantPolicy)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.TelegramLoginGrant)] IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var principal = await sessions.IssueAsync(
            externalUserIdProvider,
            request.ClientId,
            cancellationToken);
        return principal is null
            ? InvalidGrant()
            : TypedResults.SignIn(principal, authenticationScheme: Scheme);
    }

    private static ForbidHttpResult InvalidGrant() => TypedResults.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        [Scheme]);
}
