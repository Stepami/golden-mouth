using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;

namespace Reki.Api.Auth;

internal sealed class GrantTypeAuthorizationHandler(IHttpContextAccessor accessor)
    : AuthorizationHandler<GrantTypeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GrantTypeRequirement requirement)
    {
        if (accessor.HttpContext?.GetOpenIddictServerRequest()?.GrantType == requirement.GrantType)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
