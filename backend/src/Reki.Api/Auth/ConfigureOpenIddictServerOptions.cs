using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Auth;

internal sealed class ConfigureOpenIddictServerOptions(IOptions<AuthOptions> authOptions) : IConfigureOptions<OpenIddictServerOptions>
{
    public void Configure(OpenIddictServerOptions options)
    {
        var auth = authOptions.Value;
        options.Issuer = new Uri(auth.Issuer);
        options.AccessTokenLifetime = TimeSpan.FromMinutes(auth.AccessTokenMinutes);
        options.RefreshTokenLifetime = TimeSpan.FromDays(auth.RefreshTokenDays);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(auth.SigningKey);
        options.SigningCredentials.Add(new SigningCredentials(
            new RsaSecurityKey(rsa.ExportParameters(true)),
            SecurityAlgorithms.RsaSha256));
        options.EncryptionCredentials.Add(new EncryptingCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(auth.EncryptionKey)),
            SecurityAlgorithms.Aes256KW,
            SecurityAlgorithms.Aes256CbcHmacSha512));
    }
}
