using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TgMiniAppAuth;

namespace Reki.Tests;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserIdHeaderName = "X-Test-Telegram-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeaderName, out StringValues values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!long.TryParse(values.ToString(), CultureInfo.InvariantCulture, out long userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test Telegram user ID."));
        }

        string serializedUser = $$"""{"id":{{userId}},"first_name":"Test player"}""";
        string rawAuthData = $"user={Uri.EscapeDataString(serializedUser)}";
        Claim[] claims =
        [
            new Claim(TgMiniAppAuthConstants.Claims.Id, values.ToString()),
            new Claim(TgMiniAppAuthConstants.Claims.FirstName, "Test player"),
            new Claim(TgMiniAppAuthConstants.Claims.RawAuthData, rawAuthData),
        ];
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
