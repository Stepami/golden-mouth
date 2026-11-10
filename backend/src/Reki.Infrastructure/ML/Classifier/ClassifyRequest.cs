namespace Reki.Infrastructure.ML.Classifier;

public sealed record ClassifyRequest(string Model, string[] Input);