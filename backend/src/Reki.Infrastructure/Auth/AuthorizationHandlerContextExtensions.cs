using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Reki.Infrastructure.Auth;

internal static class AuthorizationHandlerContextExtensions
{
    public static bool HasGrant(this AuthorizationHandlerContext context, string grantType) =>
        context.Resource is HttpContext httpContext &&
        httpContext.GetOpenIddictServerRequest()?.GrantType == grantType;
}
