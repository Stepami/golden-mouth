using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed class ForgetStarMemoryHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<ForgetStarMemoryCommand>
{
    public async ValueTask<Unit> Handle(
        ForgetStarMemoryCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        StarConversation? conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken);
        if (conversation is not null)
        {
            await store.SaveStarConversationAsync(
                conversation with
                {
                    Memory = conversation.Memory
                        .Where(item => !string.Equals(item, request.Fact, StringComparison.Ordinal))
                        .ToArray(),
                },
                cancellationToken);
        }

        return Unit.Value;
    }
}

