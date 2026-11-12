using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram.Auth;

internal sealed class ConfigureTelegramLoginOptions(
    IOptions<TelegramOptions> telegramOptions,
    IOptions<TelegramBotClientOptions> botOptions) : IConfigureNamedOptions<JwtBearerOptions>
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
            new Uri(telegramOptions.Value.OAuthUrl),
            ".well-known/openid-configuration").ToString();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthConstants.Issuers.Telegram,
            ValidateAudience = true,
            ValidAudience = botOptions.Value.BotId.ToString(CultureInfo.InvariantCulture),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        };
    }
}