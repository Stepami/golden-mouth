using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.ML;

namespace Reki.Infrastructure.ML.Classifier;

internal sealed class InfinityClassifier(
    HttpClient client,
    IMemoryCache cache,
    IOptions<JsonSerializerOptions> jsonOptions) : IClassifier
{
    public async Task<IReadOnlyDictionary<string, double>> ClassifyAsync(
        string model,
        string text)
    {
        var cacheKey = $"{nameof(InfinityClassifier)}_{model}_{text}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, double>? cached) && cached is not null)
        {
            return cached;
        }

        using var response = await client.PostAsJsonAsync(
            "classify",
            new ClassifyRequest(model, [text]),
            jsonOptions.Value);
        response.EnsureSuccessStatusCode();
        var classifyResponse = await response.Content.ReadFromJsonAsync<ClassifyResponse>(
            jsonOptions.Value) ?? throw new InvalidOperationException("Infinity returned no classification.");

        var result = classifyResponse.Data.Single()
            .ToDictionary(
                item => item.Label,
                item => item.Score,
                StringComparer.Ordinal);
        cache.Set(cacheKey, result);
        return result;
    }
}