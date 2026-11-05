using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.AI.Embedding;

internal sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = string.Empty;
}
