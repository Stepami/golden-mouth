using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.AI;

internal sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public bool Enabled { get; init; }

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = string.Empty;
}
