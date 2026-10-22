using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public interface IStarConversationRepository
{
    ValueTask<StarConversation?> GetStarConversationAsync(
        PlayerId playerId,
        StarId starId,
        CancellationToken cancellationToken,
        WorldId worldId);

    ValueTask SaveStarConversationAsync(
        StarConversation conversation,
        CancellationToken cancellationToken);
}
