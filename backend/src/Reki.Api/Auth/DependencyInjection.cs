using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Logging;
using OpenIddict.Validation.AspNetCore;
using Reki.Infrastructure.Auth;
using Telegram.Bot;

namespace Reki.Api.Auth;

internal static class DependencyInjection
{
    public static IServiceCollection AddAppAuth(this IServiceCollection services)
    {
        services.AddKeyedScoped<IExternalUserIdProvider, TgMiniAppUserIdProvider>(AuthConstants.Grants.MiniApp);
        services.AddKeyedScoped<IExternalUserIdProvider, TgLoginUserIdProvider>(AuthConstants.Grants.TelegramLogin);

        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddRedaction();
        services.AddDownstreamDependencyMetadata<TelegramBotTelemetry>();
        services.AddHttpClient<ITelegramBotClient, RekiTelegramBotClient>()
            .AddExtendedHttpClientLogging(options =>
            {
                options.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;
                options.RequestPathLoggingMode = OutgoingPathLoggingMode.Formatted;
                options.LogBody = false;
            });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(AuthConstants.Schemes.TelegramLogin);
        services.ConfigureOptions<ConfigureTelegramLoginOptions>();
        services.ConfigureOptions<ConfigureOpenIddictServerOptions>();
        services.ConfigureOptions<ConfigureOpenIddictAspNetCoreOptions>();

        services.AddOpenIddict()
            .AddCore(options => options.UseMongoDb())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("auth/external/tg/miniapp", "auth/external/tg/widget", "auth/refresh")
                    .AllowCustomFlow(AuthConstants.Grants.MiniApp)
                    .AllowCustomFlow(AuthConstants.Grants.TelegramLogin)
                    .AllowRefreshTokenFlow()
                    .UseReferenceRefreshTokens();
                options.UseAspNetCore();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
