using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record StartChallengeRequest
{
    [Required]
    [MaxLength(64)]
    public required string FriendId { get; init; }
}

