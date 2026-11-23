using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external")]
public sealed class ExternalAuthController(IAuthSessionService sessions) : ControllerBase
{
    [HttpPost("tg/miniapp", Name = "ExchangeMiniApp")]
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

    [HttpPost("tg/widget", Name = "ExchangeTelegramLogin")]
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

    [HttpPost("vk", Name = "ExchangeVk")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.Vk)]
    [Authorize(Policy = AuthConstants.Policies.RekiVkGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeVkAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.Vk)] IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var result = await sessions.IssueAsync(externalUserIdProvider, request.ClientId, cancellationToken);
        return result.ToHttpResult();
    }

    [HttpPost("yandex", Name = "ExchangeYandex")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.Yandex)]
    [Authorize(Policy = AuthConstants.Policies.RekiYandexGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeYandexAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.Yandex)] IExternalUserIdProvider externalUserIdProvider,
        CancellationToken cancellationToken)
    {
        var result = await sessions.IssueAsync(externalUserIdProvider, request.ClientId, cancellationToken);
        return result.ToHttpResult();
    }
}