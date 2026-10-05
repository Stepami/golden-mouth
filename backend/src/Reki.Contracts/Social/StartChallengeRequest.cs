using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Social;

public sealed record StartChallengeRequest
{
    [Required]
    [MaxLength(64)]
    public required string FriendId { get; init; }
}

