using System.Globalization;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Api.Auth;

internal static class DependencyInjection
{
    public static IServiceCollection AddAppAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options =>
                Uri.TryCreate(options.Issuer, UriKind.Absolute, out _) &&
                Uri.TryCreate(options.TelegramMetadataProxyUrl, UriKind.Absolute, out _) &&
                options.AccessTokenMinutes > 0 && options.RefreshTokenDays > 0 &&
                IsValidKeys(options),
                "Auth configuration is invalid.")
            .ValidateOnStart();
        services.AddScoped<AuthSessionService>();
        services.AddMemoryCache();

        var auth = configuration.GetRequiredSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new InvalidOperationException("Auth configuration is required.");
        var telegramProxyBase = new Uri(auth.TelegramMetadataProxyUrl);
        var botApiProxyBase = new Uri(telegramProxyBase, "../api/");
        var botToken = configuration["TelegramMiniAppAuthorizationOptions:Token"]
            ?? throw new InvalidOperationException("Telegram bot token is required.");
        var botOptions = new TelegramBotClientOptions(botToken, botApiProxyBase.ToString());
        services.AddSingleton(botOptions);
        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(
            botOptions,
            new HttpClient(new TelegramBotApiProxyHandler(botApiProxyBase))));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(AuthConstants.TelegramLoginScheme, options =>
            {
                options.MapInboundClaims = false;
                options.MetadataAddress = new Uri(
                    telegramProxyBase,
                    ".well-known/openid-configuration").ToString();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://oauth.telegram.org",
                    ValidateAudience = true,
                    ValidAudience = botOptions.BotId.ToString(CultureInfo.InvariantCulture),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                };
            });

        using var rsa = RSA.Create();
        rsa.ImportFromPem(auth.SigningKey);
        var signingKey = new RsaSecurityKey(rsa.ExportParameters(true));
        var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(auth.EncryptionKey));

        services.AddOpenIddict()
            .AddCore(options => options.UseMongoDb())
            .AddServer(options =>
            {
                options.SetIssuer(auth.Issuer)
                    .SetTokenEndpointUris("auth/external/tg/miniapp", "auth/external/tg/widget", "auth/refresh")
                    .AllowCustomFlow(AuthConstants.MiniAppGrant)
                    .AllowCustomFlow(AuthConstants.TelegramLoginGrant)
                    .AllowRefreshTokenFlow()
                    .SetAccessTokenLifetime(TimeSpan.FromMinutes(auth.AccessTokenMinutes))
                    .SetRefreshTokenLifetime(TimeSpan.FromDays(auth.RefreshTokenDays))
                    .UseReferenceRefreshTokens()
                    .AddSigningKey(signingKey)
                    .AddEncryptionKey(encryptionKey);
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
                if (environment.IsDevelopment())
                {
                    options.UseAspNetCore().DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    private static bool IsValidKeys(AuthOptions auth)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(auth.SigningKey);
            return rsa.KeySize >= 2048 && Convert.FromBase64String(auth.EncryptionKey).Length == 32;
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or FormatException)
        {
            return false;
        }
    }
}
