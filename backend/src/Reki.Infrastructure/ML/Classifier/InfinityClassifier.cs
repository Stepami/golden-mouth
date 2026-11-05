using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Reki.Application.Posts.ML;

namespace Reki.Infrastructure.ML.Classifier;

internal sealed class InfinityClassifier(
    HttpClient client,
    IOptions<JsonSerializerOptions> jsonOptions) : IClassifier
{
    public async Task<IReadOnlyDictionary<string, double>> ClassifyAsync(
        string model,
        string text,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "classify",
            new ClassifyRequest(model, [text]),
            jsonOptions.Value,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClassifyResponse>(
            jsonOptions.Value,
            cancellationToken) ?? throw new InvalidOperationException("Infinity returned no classification.");
        return result.Data.Single().ToDictionary(item => item.Label, item => item.Score, StringComparer.Ordinal);
    }
}