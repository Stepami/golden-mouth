using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Requests.Auth;

internal static class IssueResultMapper
{
    public static Results<SignInHttpResult, ForbidHttpResult> ToHttpResult(this IssueResult result)
    {
        const string scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
        if (result is { Ok: true, Identity: { } identity })
        {
            return TypedResults.SignIn(identity, authenticationScheme: scheme);
        }

        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = result.Error,
        });
        if (result.Identity?.GetClaim(AuthConstants.Claims.ExternalUserId) is { } externalUserId)
        {
            properties.SetParameter(AuthConstants.Claims.ExternalUserId, Convert.ToBase64String(Encoding.UTF8.GetBytes(externalUserId)));
        }

        return TypedResults.Forbid(properties, [scheme]);
    }
}
