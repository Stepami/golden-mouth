using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;

namespace Reki.Infrastructure.Cache;

public static class DependencyInjection
{
    public static IServiceCollection AddCache(
       this IServiceCollection services)
    {
        services.TryAddSingleton<IMemoryCache, RekiMemoryCache>();
        services.AddKeyedSingleton<ISystemClock, TimeProviderSystemClock>(
            nameof(TimeProviderSystemClock));
        services.ConfigureOptions<ConfigureMemoryCacheClock>();

        return services;
    }
}