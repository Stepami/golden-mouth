using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Player.DeletePlayerCommand;
using Reki.Application.Player.UpdateLastSeenAtCommand;

namespace Reki.Application.Player.MergePlayersCommand;

internal sealed class MergePlayersCommandHandler(
    IPlayerRepository players,
    ISender sender) : ICommandHandler<MergePlayers>
{
    public async ValueTask<Unit> Handle(MergePlayers command, CancellationToken cancellationToken)
    {
        if (command.FromPlayerId == command.ToPlayerId)
        {
            throw new RequestConflictException("A player cannot be merged with itself.");
        }

        var source = await players.GetPlayerAsync(command.FromPlayerId, cancellationToken)
                     ?? throw new ResourceNotFoundException("Player not found.");
        var target = await players.GetPlayerAsync(command.ToPlayerId, cancellationToken)
                     ?? throw new ResourceNotFoundException("Player not found.");
        await sender.Send(new DeletePlayer(command.FromPlayerId), cancellationToken);
        var updated = target.AddExternalUserIds(source.ExternalUserIds);
        await players.ReplacePlayerAsync(updated, cancellationToken);
        await sender.Send(new UpdateLastSeenAt(updated, Force: true), cancellationToken);
        return Unit.Value;
    }
}