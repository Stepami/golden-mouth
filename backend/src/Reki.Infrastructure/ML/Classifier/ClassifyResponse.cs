namespace Reki.Infrastructure.ML.Classifier;

public sealed record ClassifyResponse(ClassifyResponse.ClassifyScore[][] Data)
{
    public sealed record ClassifyScore(string Label, double Score);
}
