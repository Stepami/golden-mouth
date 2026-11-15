using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.Cache;

internal sealed class ConfigureMemoryCacheClock(
    [FromKeyedServices(nameof(TimeProviderSystemClock))]
    ISystemClock systemClock) : IConfigureOptions<MemoryCacheOptions>
{
    public void Configure(MemoryCacheOptions options)
    {
        options.Clock = systemClock;
        options.TrackStatistics = true;
    }
}