using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Posts;

/// <summary>Starts streaming generation for a publication identified by a client-generated UUID.</summary>
public sealed record StartPublicationRequest
{
    [Required]
    public required string PublicationId { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; init; }

    public bool OwnWords { get; init; } = true;

    [StringLength(64)]
    public string? WantId { get; init; }
}
