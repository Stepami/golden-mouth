using Mediator;
using Reki.Application.Notifications;
using Reki.Application.RekiContext;
using Reki.Domain.Notifications;
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
        var channel = PlayerNotificationChannel.WebPush(command.Endpoint, command.P256dh, command.Auth);
        var updated = player.AddNotificationChannel(channel);
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await players.ReplacePlayerAsync(updated, token);
                if (await notifications.GetPendingAsync(player.Id, token) is null)
                {
                    await notifications.InsertAsync(
                        Notification.New(player.Id, timeProvider.GetUtcNow().AddDays(1)),
                        token);
                }

                return true;
            },
            cancellationToken);
        return Unit.Value;
    }
}