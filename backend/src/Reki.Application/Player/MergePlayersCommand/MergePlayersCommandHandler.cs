using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Player.DeletePlayerCommand;

namespace Reki.Application.Player.MergePlayersCommand;

internal sealed class MergePlayersCommandHandler(
    IPlayerRepository players,
    ISender sender) : ICommandHandler<MergePlayers>
{
    public async ValueTask<Unit> Handle(MergePlayers request, CancellationToken cancellationToken)
    {
        if (request.FromPlayerId == request.ToPlayerId)
        {
            throw new RequestConflictException("A player cannot be merged with itself.");
        }

        var source = await players.GetPlayerAsync(request.FromPlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        var target = await players.GetPlayerAsync(request.ToPlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        await sender.Send(new DeletePlayer(request.FromPlayerId), cancellationToken);
        await players.ReplacePlayerAsync(target.AddExternalUserIds(source.ExternalUserIds), cancellationToken);
        return Unit.Value;
    }
}
