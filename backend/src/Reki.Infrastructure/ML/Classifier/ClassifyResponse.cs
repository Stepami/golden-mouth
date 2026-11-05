namespace Reki.Infrastructure.ML.Classifier;

#pragma warning disable S2368
public sealed record ClassifyResponse(ClassifyResponse.ClassifyScore[][] Data)
{
    public sealed record ClassifyScore(string Label, double Score);
}
#pragma warning restore S2368
