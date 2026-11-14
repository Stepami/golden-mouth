using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace Reki.Infrastructure.AI.Embedding;

internal sealed class MemoryCachingEmbeddingGenerator(
    IMemoryCache cache,
    IEmbeddingGenerator<string, Embedding<float>> innerGenerator) :
    CachingEmbeddingGenerator<string, Embedding<float>>(innerGenerator)
{
    protected override string GetCacheKey(params ReadOnlySpan<object?> values)
    {
        return AIJsonUtilities.HashDataToString(values, AIJsonUtilities.DefaultOptions);
    }

    protected override Task<Embedding<float>?> ReadCacheAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(cache.Get<Embedding<float>>(key));
    }

    protected override Task WriteCacheAsync(string key, Embedding<float> value, CancellationToken cancellationToken)
    {
        cache.Set(key, value);
        return Task.CompletedTask;
    }
}