namespace Reki.Application.Posts.ML;

public interface IClassifier
{
    Task<IReadOnlyDictionary<string, double>> ClassifyAsync(
        string model,
        string text);
}

internal static class ClassifierModels
{
    public const string ToxicityModel = "toxicity";

    public const string EmotionModel = "emotion";
}