using Mediator;
using Reki.Application.Player.UpdateLastSeenAtCommand;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.RegisterWebPushCommand;

internal sealed class RegisterWebPushCommandHandler(
    IRekiContextAccessor context,
    IPlayerRepository players,
    ISender sender) : ICommandHandler<RegisterWebPush>
{
    public async ValueTask<Unit> Handle(RegisterWebPush command, CancellationToken cancellationToken)
    {
        var player = await context.Context.Player;
        var channel = PlayerNotificationChannel.WebPush(command.Endpoint, command.P256dh, command.Auth);
        var updated = player.AddNotificationChannel(channel);
        await players.ReplacePlayerAsync(updated, cancellationToken);
        await sender.Send(new UpdateLastSeenAt(updated, Force: true), cancellationToken);
        return Unit.Value;
    }
}