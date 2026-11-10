using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Reki.Api.Requests.Auth;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/vk")]
public sealed class ExternalVkAuthController(IAuthSessionService sessions) : ControllerBase
{
    [HttpPost(Name = "ExchangeVk")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.Vk)]
    [Authorize(Policy = AuthConstants.Policies.RekiVkGrant)]
    public async Task<Results<SignInHttpResult, ForbidHttpResult>> ExchangeVkAsync(
        [FromForm] TokenExchangeRequest request,
        [FromKeyedServices(AuthConstants.Grants.Vk)] IExternalUserIdProvider externalUserIdProvider,
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
            : TypedResults.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ForbidHttpResult InvalidGrant(string error, string? externalId)
    {
        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
        });
        if (externalId is not null)
        {
            properties.SetParameter(AuthConstants.Claims.ExternalUserId, Convert.ToBase64String(Encoding.UTF8.GetBytes(externalId)));
        }

        return TypedResults.Forbid(properties, [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
    }
}
