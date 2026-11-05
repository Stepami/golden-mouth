namespace Reki.Infrastructure.AI;

internal sealed class NliOptions
{
    public const string SectionName = "Nli";

    public bool Enabled { get; init; }

    public string ModelPath { get; init; } = string.Empty;

    public string VocabPath { get; init; } = string.Empty;
}
