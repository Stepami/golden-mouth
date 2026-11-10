using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;

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
        var result = await sessions.IssueAsync(externalUserIdProvider, request.ClientId, cancellationToken);
        return result.ToHttpResult();
    }
}
