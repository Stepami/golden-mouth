using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using Reki.Application.Auth;
using Reki.Infrastructure.Auth;
using TgMiniAppAuth;
using TgMiniAppAuth.Authentication;

namespace Reki.Infrastructure.Telegram.Auth;

internal static class DependencyInjection
{
    public static IServiceCollection AddTelegramAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTgMiniAppAuth(configuration);
        services.RemoveAll<IConfigureOptions<TelegramMiniAppAuthorizationOptions>>();
        services.AddTransient<IOptionsFactory<TelegramMiniAppAuthorizationOptions>, TelegramMiniAppOptionsFactory>();

        services.AddKeyedScoped<IExternalUserIdProvider, TgMiniAppUserIdProvider>(AuthConstants.Grants.MiniApp);
        services.AddKeyedScoped<IExternalUserIdProvider, TgLoginUserIdProvider>(AuthConstants.Grants.TelegramLogin);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(AuthConstants.Schemes.TelegramLogin);
        services.ConfigureOptions<ConfigureTelegramLoginOptions>();

        return services;
    }
}
