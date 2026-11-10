using System.Text;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Requests.Auth;
using Reki.Api.Requests.Player;
using Reki.Application.Auth;
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
        var externalId = await externalUserIdProvider.GetExternalUserIdOrNullAsync(cancellationToken);
        if (externalId is null)
        {
            return InvalidGrant(Errors.InvalidGrant, null);
        }

        var principal = await sessions.IssueAsync(externalId.Value, request.ClientId, cancellationToken);
        return principal is null
            ? InvalidGrant("registration_required", externalId.Value.Value)
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
        var externalId = await externalUserIdProvider.GetExternalUserIdOrNullAsync(cancellationToken);
        if (externalId is null)
        {
            return InvalidGrant(Errors.InvalidGrant, null);
        }

        var principal = await sessions.IssueAsync(externalId.Value, request.ClientId, cancellationToken);
        return principal is null
            ? InvalidGrant("registration_required", externalId.Value.Value)
            : TypedResults.SignIn(principal, authenticationScheme: Scheme);
    }

    [HttpPost("/onboarding", Name = "CompleteOnboarding")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.Onboarding)]
    public async Task<Ok> CompleteOnboardingAsync(
        OnboardingRequest request,
        [FromServices] ISender sender,
        [FromKeyedServices(AuthConstants.Schemes.Onboarding)] IExternalUserIdProvider provider,
        CancellationToken cancellationToken)
    {
        var externalId = await provider.GetExternalUserIdOrNullAsync(cancellationToken);
        if (externalId is null)
        {
            throw new InvalidOperationException("Onboarding identity is missing.");
        }

        await sender.Send(request.ToOnboardPlayer(externalId.Value), cancellationToken);
        return TypedResults.Ok();
    }

    private static ForbidHttpResult InvalidGrant(string error, string? externalUserId)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
        });
        if (externalUserId is not null)
        {
            properties.SetParameter(
                AuthConstants.Claims.ExternalUserId,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(externalUserId)));
        }

        return TypedResults.Forbid(properties, [Scheme]);
    }
}
