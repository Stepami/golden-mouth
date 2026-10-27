using System.ComponentModel.DataAnnotations;
using Reki.Domain.Player;

namespace Reki.Api.Requests.Social;

public sealed record StartChallengeRequest
{
    [Required]
    public required PlayerId FriendId { get; init; }
}

