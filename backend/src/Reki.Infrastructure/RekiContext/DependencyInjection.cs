using Microsoft.Extensions.DependencyInjection;
using Reki.Application.RekiContext;

namespace Reki.Infrastructure.RekiContext;

internal static class DependencyInjection
{
    public static IServiceCollection AddRekiContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IRekiContextAccessor, HttpRekiContextAccessor>();
        services.AddScoped<IRekiContext, LazyRekiContext>();

        return services;
    }
}
