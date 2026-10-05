using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using TgMiniAppAuth;

namespace Reki.Api.Authentication;

internal sealed class DevelopmentTelegramAuthenticationGuard(
    RequestDelegate next,
    IHostEnvironment environment)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (!ShouldRejectDevelopmentMarker(context.Request))
        {
            return next(context);
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    private bool ShouldRejectDevelopmentMarker(HttpRequest request)
    {
        if (environment.IsDevelopment() ||
            !AuthenticationHeaderValue.TryParse(
                request.Headers.Authorization.ToString(),
                out AuthenticationHeaderValue? authorization) ||
            !string.Equals(
                authorization.Scheme,
                TgMiniAppAuthConstants.AuthenticationScheme,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            return false;
        }

        var values = QueryHelpers.ParseQuery(authorization.Parameter);
        return values.TryGetValue("hash", out var hash) &&
            string.Equals(hash.ToString(), "development", StringComparison.Ordinal);
    }
}
