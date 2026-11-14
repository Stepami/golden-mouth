using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Telemetry.Landing;

public sealed record LandingVisitRequest
{
    [Required]
    [RegularExpression("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")]
    public required string VisitorId { get; init; }

    [Required]
    [RegularExpression("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")]
    public required string EntryId { get; init; }

    [Required]
    [RegularExpression("^(worlds|hogwarts)$")]
    public required string Variant { get; init; }

    [Required]
    [RegularExpression("^(worlds|hogwarts)$")]
    public required string InitialVariant { get; init; }

    [Required]
    [RegularExpression("^(link|direct)$")]
    public required string Source { get; init; }

    public required bool Switched { get; init; }

    [MaxLength(64)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$")]
    public string? Campaign { get; init; }

    [MaxLength(64)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$")]
    public string? Medium { get; init; }

    [MaxLength(64)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$")]
    public string? TrafficSource { get; init; }

    [MaxLength(64)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$")]
    public string? Content { get; init; }
}
