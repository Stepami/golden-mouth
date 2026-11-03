using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Auth;
using Reki.Api.Requests.Auth;
using Telegram.Bot;
using TgMiniAppAuth.AuthContext.User;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/tg")]
public sealed class ExternalTelegramAuthController(
    AuthSessionService sessions,
    ITelegramBotClient bot) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("client-id", Name = "GetTelegramClientId")]
    public Ok<long> GetTelegramClientId() => TypedResults.Ok(bot.BotId);

    [HttpPost("miniapp", Name = "ExchangeMiniApp")]
    [Authorize(AuthenticationSchemes = "TMiniApp", Policy = "TMiniApp")]
    [Authorize(Policy = AuthConstants.MiniAppGrantPolicy)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeMiniAppAsync(
        [FromForm] TokenExchangeRequest request,
        [FromServices] ITelegramUserAccessor telegramUser,
        CancellationToken cancellationToken)
    {
        var principal = await sessions.IssueAsync(telegramUser.User.Id, request.ClientId, cancellationToken);
        return principal is null ? InvalidGrant() : TypedResults.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("widget", Name = "ExchangeTelegramLogin")]
    [Authorize(AuthenticationSchemes = AuthConstants.TelegramLoginScheme)]
    [Authorize(Policy = AuthConstants.TelegramLoginGrantPolicy)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeTelegramLoginAsync(
        [FromForm] TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue("id"), NumberStyles.None, CultureInfo.InvariantCulture, out var telegramId) ||
            telegramId <= 0)
        {
            return InvalidGrant();
        }

        var principal = await sessions.IssueAsync(telegramId, request.ClientId, cancellationToken);
        return principal is null ? InvalidGrant() : TypedResults.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ForbidHttpResult InvalidGrant() => TypedResults.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
        }),
        [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
