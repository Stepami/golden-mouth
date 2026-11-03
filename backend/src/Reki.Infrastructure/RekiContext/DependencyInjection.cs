using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reki.Application.RekiContext;
using Reki.Infrastructure.Auth;
using TgMiniAppAuth;
using TgMiniAppAuth.Authentication;

namespace Reki.Infrastructure.RekiContext;

internal static class DependencyInjection
{
    public static IServiceCollection AddRekiContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>().BindConfiguration(TelegramOptions.SectionName);
        services.AddTgMiniAppAuth(configuration);
        services.RemoveAll<IConfigureOptions<TelegramMiniAppAuthorizationOptions>>();
        services.AddTransient<IOptionsFactory<TelegramMiniAppAuthorizationOptions>, TelegramMiniAppOptionsFactory>();
        services.AddSingleton<IRekiContextAccessor, HttpRekiContextAccessor>();
        services.AddScoped<IRekiContext, LazyRekiContext>();

        return services;
    }
}
