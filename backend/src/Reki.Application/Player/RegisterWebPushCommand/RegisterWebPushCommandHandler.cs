using Mediator;
using Reki.Application.Notifications;
using Reki.Application.RekiContext;
using Reki.Domain.Player;

namespace Reki.Application.Player.RegisterWebPushCommand;

internal sealed class RegisterWebPushCommandHandler(
    IRekiContextAccessor context,
    IPlayerRepository players,
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<RegisterWebPush>
{
    public async ValueTask<Unit> Handle(RegisterWebPush command, CancellationToken cancellationToken)
    {
        var player = await context.Context.Player;
        var channel = new PlayerNotificationChannel(
            NotificationChannelType.WebPush,
            new Dictionary<string, string>
                { ["endpoint"] = command.Endpoint, ["p256dh"] = command.P256dh, ["auth"] = command.Auth });
        var updated = player with
        {
            NotificationChannels = NotificationSubscriptions.WithChannel(player.NotificationChannels, channel),
        };
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(updated, token);
                if (await notifications.GetPendingAsync(player.Id, token) is null)
                {
                    await notifications.InsertAsync(
                        NotificationSchedule.Create(player.Id, timeProvider.GetUtcNow()),
                        token);
                }

                return true;
            },
            cancellationToken);
        return Unit.Value;
    }
}