using Reki.Domain.Player;

namespace Reki.Api.Requests.Player;

public sealed record MergePlayersRequest
{
    public required PlayerId FromPlayerId { get; init; }
}
