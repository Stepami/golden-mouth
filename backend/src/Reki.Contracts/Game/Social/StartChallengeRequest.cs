using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Game.Social;

public sealed record StartChallengeRequest
{
    [Required]
    [MaxLength(64)]
    public required string FriendId { get; init; }
}

