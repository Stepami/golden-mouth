using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using Reki.Infrastructure.Auth;
using Telegram.Bot;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(IAuthSessionService sessions) : ControllerBase
{
    private const string Scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;

    [AllowAnonymous]
    [HttpGet("client-id", Name = "GetTelegramClientId")]
    public Ok<long> GetTelegramClientId(
        [FromServices] ITelegramBotClient bot) =>
        TypedResults.Ok(bot.BotId);

    [HttpPost("miniapp", Name = "ExchangeMiniApp")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.MiniApp, Policy = AuthConstants.Policies.MiniApp)]
    [Authorize(Policy = AuthConstants.Policies.RekiMiniAppGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeMiniAppAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.MiniApp)] IExternalUserIdProvider externalUserIdProvider,
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
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.TelegramLogin)]
    [Authorize(Policy = AuthConstants.Policies.RekiTelegramLoginGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.TelegramLogin)] IExternalUserIdProvider externalUserIdProvider,
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
