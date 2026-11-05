using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.ML;

internal sealed partial class InfinityClassifierClient(HttpClient client) : ITextSignalsScorer
{
    public async ValueTask<TextSignals> ScoreAsync(string text, CancellationToken cancellationToken)
    {
        var toxicity = ClassifyAsync("toxicity", text, cancellationToken);
        var emotion = ClassifyAsync("emotion", text, cancellationToken);
        await Task.WhenAll(toxicity, emotion);
        return new TextSignals(await toxicity, await emotion);
    }

    private async Task<IReadOnlyDictionary<string, double>> ClassifyAsync(
        string model,
        string text,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "classify",
            new ClassifyRequest(model, [text]),
            ClassifierJsonContext.Default.ClassifyRequest,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync(
            ClassifierJsonContext.Default.ClassifyResponse,
            cancellationToken) ?? throw new InvalidOperationException("Infinity returned no classification.");
        return result.Data.Single().ToDictionary(item => item.Label, item => item.Score, StringComparer.Ordinal);
    }

    internal sealed record ClassifyRequest(string Model, string[] Input);

    internal sealed record ClassifyResponse(ClassifyScore[][] Data);

    internal sealed record ClassifyScore(string Label, double Score);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ClassifyRequest))]
    [JsonSerializable(typeof(ClassifyResponse))]
    internal sealed partial class ClassifierJsonContext : JsonSerializerContext;
}
