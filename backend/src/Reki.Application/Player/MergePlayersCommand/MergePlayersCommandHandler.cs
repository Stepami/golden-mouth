using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Notifications;
using Reki.Application.Player.DeletePlayerCommand;
using Reki.Domain.Notifications;

namespace Reki.Application.Player.MergePlayersCommand;

internal sealed class MergePlayersCommandHandler(
    IPlayerRepository players,
    INotificationRepository notifications,
    ISender sender,
    TimeProvider timeProvider) : ICommandHandler<MergePlayers>
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
        if (updated.NotificationChannels.Count > 0 &&
            await notifications.GetPendingAsync(target.Id, cancellationToken) is null)
        {
            await notifications.InsertAsync(
                Notification.New(target.Id, timeProvider.GetUtcNow().AddDays(1)),
                cancellationToken);
        }

        return Unit.Value;
    }
}