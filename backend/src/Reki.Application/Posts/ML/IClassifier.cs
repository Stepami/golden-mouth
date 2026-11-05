namespace Reki.Application.Posts.ML;

public interface IClassifier
{
    Task<IReadOnlyDictionary<string, double>> ClassifyAsync(
        string model,
        string text,
        CancellationToken cancellationToken);
}

internal static class ClassifierModels
{
    public const string ToxicityModel = "toxicity";

    public const string EmotionModel = "emotion";
}