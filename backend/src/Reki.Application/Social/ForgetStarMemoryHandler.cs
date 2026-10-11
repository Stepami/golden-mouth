using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;

namespace Reki.Application.Social;

public sealed class ForgetStarMemoryHandler(
    IGameStore store,
    ISender sender)
    : IRequestHandler<ForgetStarMemoryCommand>
{
    public async ValueTask<Unit> Handle(
        ForgetStarMemoryCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        _ = StarCatalog.Require(player.World, request.StarId);
        var conversation = await store.GetStarConversationAsync(
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

