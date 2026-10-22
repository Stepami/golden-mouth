using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.RekiContext;
using TgMiniAppAuth;

namespace Reki.Infrastructure.RekiContext;

internal static class DependencyInjection
{
    public static IServiceCollection AddRekiContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTgMiniAppAuth(configuration);
        services.AddSingleton<IRekiContextAccessor, HttpRekiContextAccessor>();
        services.AddScoped<IRekiContext, LazyRekiContext>();

        return services;
    }
}
