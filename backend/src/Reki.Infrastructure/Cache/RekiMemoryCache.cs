using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Reki.Infrastructure.Cache;

internal sealed class RekiMemoryCache(
    IOptions<MemoryCacheOptions> optionsAccessor,
    ILoggerFactory loggerFactory) : IMemoryCache
{
    private readonly MemoryCache _memoryCache = new(
        optionsAccessor,
        loggerFactory);

    public bool TryGetValue(object key, out object? value)
    {
        return _memoryCache.TryGetValue(key, out value);
    }

    public ICacheEntry CreateEntry(object key)
    {
        var cacheEntry = _memoryCache.CreateEntry(key);
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
        return cacheEntry;
    }

    public void Remove(object key)
    {
        _memoryCache.Remove(key);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}