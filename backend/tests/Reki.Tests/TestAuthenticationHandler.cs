using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TgMiniAppAuth;

namespace Reki.Tests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-Telegram-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out StringValues userValue) ||
            !long.TryParse(userValue, out long userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string serializedUser = $$"""{"id":{{userId}},"first_name":"Test player"}""";
        string rawAuthData = $"user={Uri.EscapeDataString(serializedUser)}";
        Claim[] claims =
        [
            new(TgMiniAppAuthConstants.Claims.Id, userId.ToString(CultureInfo.InvariantCulture)),
            new(TgMiniAppAuthConstants.Claims.FirstName, "Test player"),
            new(TgMiniAppAuthConstants.Claims.RawAuthData, rawAuthData),
        ];
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
