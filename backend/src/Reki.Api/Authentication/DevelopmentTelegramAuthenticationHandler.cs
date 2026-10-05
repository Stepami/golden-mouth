using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TgMiniAppAuth;

namespace Reki.Api.Authentication;

internal sealed class DevelopmentTelegramAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentTelegram";

    private const string DevelopmentHash = "development";
    private const string InvalidInitDataMessage = "Invalid development Telegram init data.";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment() ||
            !AuthenticationHeaderValue.TryParse(
                Request.Headers.Authorization.ToString(),
                out AuthenticationHeaderValue? authorization) ||
            !string.Equals(
                authorization.Scheme,
                TgMiniAppAuthConstants.AuthenticationScheme,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            Dictionary<string, Microsoft.Extensions.Primitives.StringValues> values =
                QueryHelpers.ParseQuery(authorization.Parameter);
            if (!values.TryGetValue("hash", out var hash) ||
                !string.Equals(hash.ToString(), DevelopmentHash, StringComparison.Ordinal) ||
                !values.TryGetValue("auth_date", out var authDate) ||
                !long.TryParse(
                    authDate.ToString(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long authDateSeconds) ||
                authDateSeconds <= 0 ||
                !values.TryGetValue("user", out var serializedUser))
            {
                return Task.FromResult(AuthenticateResult.Fail(InvalidInitDataMessage));
            }

            using JsonDocument document = JsonDocument.Parse(serializedUser.ToString());
            JsonElement user = document.RootElement;
            if (user.ValueKind != JsonValueKind.Object ||
                !user.TryGetProperty("id", out JsonElement idElement) ||
                !idElement.TryGetInt64(out long userId) ||
                userId <= 0 ||
                !user.TryGetProperty("first_name", out JsonElement firstNameElement) ||
                firstNameElement.ValueKind != JsonValueKind.String ||
                firstNameElement.GetString() is not { } firstName ||
                string.IsNullOrWhiteSpace(firstName) ||
                firstName.Length > 64)
            {
                return Task.FromResult(AuthenticateResult.Fail(InvalidInitDataMessage));
            }

            Claim[] claims =
            [
                new Claim(
                    TgMiniAppAuthConstants.Claims.Id,
                    userId.ToString(CultureInfo.InvariantCulture)),
                new Claim(TgMiniAppAuthConstants.Claims.FirstName, firstName),
                new Claim(
                    TgMiniAppAuthConstants.Claims.RawAuthData,
                    authorization.Parameter),
            ];
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (JsonException)
        {
            return Task.FromResult(AuthenticateResult.Fail(InvalidInitDataMessage));
        }
    }
}
