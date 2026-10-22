using System.ComponentModel.DataAnnotations;

namespace Reki.Api.Requests.Social;

public sealed record StartChallengeRequest
{
    [Required]
    [MaxLength(64)]
    public required string FriendId { get; init; }
}

