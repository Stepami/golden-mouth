using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Auth;

internal sealed class OnboardingBasicHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            var raw = new UTF8Encoding(false, true).GetString(Convert.FromBase64String(header[6..]));
            if (!ExternalUserId.TryFrom(raw, out var externalId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid external identity."));
            }

            var identity = new ClaimsIdentity([new Claim(AuthConstants.Claims.ExternalUserId, externalId.Value)], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid external identity."));
        }
    }
}
