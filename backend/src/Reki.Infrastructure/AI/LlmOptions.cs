using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.AI;

internal sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 20;
}
