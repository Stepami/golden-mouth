using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Telemetry.Landing;

public sealed record LandingEntryEventRequest
{
    [Required]
    [RegularExpression("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$")]
    public required string EventId { get; init; }

    [Required]
    [RegularExpression("^(landing_view|landing_continue|landing_auth_complete|landing_world_selected|landing_character_selected|landing_reaction_viewed)$")]
    public required string Name { get; init; }

    [Range(1, long.MaxValue)]
    public required long Timestamp { get; init; }

    [Required]
    public required LandingVisitRequest Visit { get; init; }

    public Guid? PostId { get; init; }
}
