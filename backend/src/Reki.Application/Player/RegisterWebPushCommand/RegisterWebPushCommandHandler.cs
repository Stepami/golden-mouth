using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.RegisterWebPushCommand;

internal sealed class RegisterWebPushCommandHandler(
    IRekiContextAccessor context,
    IPlayerRepository players,
    IMemoryCache cache) : ICommandHandler<RegisterWebPush>
{
    public async ValueTask<Unit> Handle(RegisterWebPush command, CancellationToken cancellationToken)
    {
        var player = await context.Context.Player;
        var channel = PlayerNotificationChannel.WebPush(command.Endpoint, command.P256dh, command.Auth);
        var updated = player.AddNotificationChannel(channel);
        await players.ReplacePlayerAsync(updated, cancellationToken);
        cache.Remove(player.Id);
        return Unit.Value;
    }
}