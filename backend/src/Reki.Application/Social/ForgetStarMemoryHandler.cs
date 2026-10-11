using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
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
        var world = await sender.Send(new Reki.Application.Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var conversation = await store.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken,
            world.Id);
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