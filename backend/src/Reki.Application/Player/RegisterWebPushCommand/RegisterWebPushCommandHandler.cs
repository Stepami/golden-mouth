using Mediator;
using Reki.Application.Player.UpdateLastSeenAtCommand;
using Reki.Application.RekiContext;
using Reki.Domain.Notifications;

namespace Reki.Application.Player.RegisterWebPushCommand;

internal sealed class RegisterWebPushCommandHandler(
    IRekiContextAccessor context,
    IPlayerRepository players,
    ISender sender) : ICommandHandler<RegisterWebPush>
{
    public async ValueTask<Unit> Handle(RegisterWebPush command, CancellationToken cancellationToken)
    {
        var player = await context.Context.Player;
        var channel = player.NotificationChannels.GetValueOrDefault(NotificationChannelType.WebPush) as WebPushNotificationChannel;
        var subscription = new WebPushSubscription(command.Endpoint, command.P256dh, command.Auth);
        if (channel is null ||
            !channel.Subscriptions.TryGetValue(subscription.Endpoint, out var existing) || existing != subscription)
        {
            var subscriptions = channel is null
                ? new Dictionary<string, WebPushSubscription>(StringComparer.Ordinal)
                : new Dictionary<string, WebPushSubscription>(channel.Subscriptions, StringComparer.Ordinal);
            subscriptions[subscription.Endpoint] = subscription;
            player.AddNotificationChannel(new WebPushNotificationChannel(subscriptions));
            await players.ReplacePlayerAsync(player, cancellationToken);
        }

        await sender.Send(new UpdateLastSeenAt(player, Force: true), cancellationToken);
        return Unit.Value;
    }
}
