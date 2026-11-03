using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Api.Auth;

internal sealed class ConfigureTelegramLoginOptions(
    IOptions<AuthOptions> authOptions,
    ITelegramBotClient bot) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(JwtBearerOptions options) => Configure(AuthConstants.Schemes.TelegramLogin, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != AuthConstants.Schemes.TelegramLogin)
        {
            return;
        }

        options.MapInboundClaims = false;
        options.MetadataAddress = new Uri(
            new Uri(authOptions.Value.TelegramMetadataProxyUrl),
            ".well-known/openid-configuration").ToString();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthConstants.Issuers.Telegram,
            ValidateAudience = true,
            ValidAudience = bot.BotId.ToString(CultureInfo.InvariantCulture),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        };
    }
}
