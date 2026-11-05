using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.AI;

internal sealed partial class InfinityNliClient(HttpClient client) : INliScorer
{
    public async ValueTask<IReadOnlyList<NliScores>> ScoreAsync(
        string premise,
        IReadOnlyList<string> hypotheses,
        CancellationToken cancellationToken)
    {
        const int windowLength = 700;
        const int windowStep = 600;
        var windows = Enumerable.Range(0, (premise.Length + windowStep - 1) / windowStep)
            .Select(index => premise.Substring(index * windowStep, Math.Min(windowLength, premise.Length - (index * windowStep))));
        var input = windows.SelectMany(window => hypotheses.Select(hypothesis => $"{window}<eos>{hypothesis}"))
            .ToArray();
        using var response = await client.PostAsJsonAsync(
            "classify",
            new ClassifyRequest("horizon", input),
            InfinityNliJsonContext.Default.ClassifyRequest,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync(
            InfinityNliJsonContext.Default.ClassifyResponse,
            cancellationToken) ?? throw new InvalidOperationException("Infinity returned no classification.");
        return Enumerable.Range(0, hypotheses.Count)
            .Select(index => result.Data
                .Where((_, row) => row % hypotheses.Count == index)
                .Select(scores => new NliScores(
                    scores.Single(score => score.Label == "entailment").Score,
                    scores.Single(score => score.Label == "not_entailment").Score))
                .OrderByDescending(scores => scores.Entailment)
                .First())
            .ToArray();
    }

    internal sealed record ClassifyRequest(string Model, string[] Input);

    internal sealed record ClassifyResponse(ClassifyScore[][] Data);

    internal sealed record ClassifyScore(string Label, double Score);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ClassifyRequest))]
    [JsonSerializable(typeof(ClassifyResponse))]
    internal sealed partial class InfinityNliJsonContext : JsonSerializerContext;
}
