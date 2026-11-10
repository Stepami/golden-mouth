using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("auth/external/yandex")]
public sealed class ExternalYandexAuthController(IAuthSessionService sessions) : ControllerBase
{
    [HttpPost(Name = "ExchangeYandex")]
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
