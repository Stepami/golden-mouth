namespace Reki.Infrastructure.ML;

internal sealed class NliOptions
{
    public const string SectionName = "Nli";

    public string ModelPath { get; init; } = string.Empty;

    public string VocabPath { get; init; } = string.Empty;
}
