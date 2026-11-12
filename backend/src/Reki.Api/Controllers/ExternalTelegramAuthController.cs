using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(IAuthSessionService sessions) : ControllerBase
{
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
        [FromKeyedServices(AuthConstants.Grants.MiniApp)]
        IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var result = await sessions.IssueAsync(externalUserIdProvider, request.ClientId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("widget", Name = "ExchangeTelegramLogin")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.TelegramLogin)]
    [Authorize(Policy = AuthConstants.Policies.RekiTelegramLoginGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.TelegramLogin)]
        IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var result = await sessions.IssueAsync(externalUserIdProvider, request.ClientId, cancellationToken);
        return result.ToHttpResult();
    }
}