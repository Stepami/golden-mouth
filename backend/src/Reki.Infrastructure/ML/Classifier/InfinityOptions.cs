using System.ComponentModel.DataAnnotations;

namespace Reki.Infrastructure.ML.Classifier;

internal sealed class InfinityOptions
{
    public const string SectionName = "Infinity";

    [Required]
    public string BaseUrl { get; init; } = string.Empty;
}