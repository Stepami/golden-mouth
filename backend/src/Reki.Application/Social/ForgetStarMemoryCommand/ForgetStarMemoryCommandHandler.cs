using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Social.ForgetStarMemoryCommand;

public sealed class ForgetStarMemoryCommandHandler(
    IStarConversationRepository starConversationRepository,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<ForgetStarMemory>
{
    public async ValueTask<Unit> Handle(
        ForgetStarMemory request,
        CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        _ = world.FindStar(request.StarId) ?? throw new ResourceNotFoundException("The star does not belong to this world.");
        var conversation = await starConversationRepository.GetStarConversationAsync(
            player.Id,
            request.StarId,
            cancellationToken,
            world.Id);
        if (conversation is not null)
        {
            await starConversationRepository.SaveStarConversationAsync(
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
