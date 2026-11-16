using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Reki.Domain.Notifications;
using Reki.Domain.Player;

namespace Reki.Application.Notifications;

public abstract partial class NotificationSender(
    NotificationChannelType channelType,
    ILogger<NotificationSender> logger,
    TimeProvider timeProvider) : INotificationSender
{
    public async Task SendAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
    {
        if (channel.Type != channelType)
        {
            return;
        }

        try
        {
            await SendInternalAsync(channel, notification, ct);

            // Only the routing tag belongs in the scope; event data comes from the message template.
            using var scope = logger.BeginScope(new Dictionary<string, object?> { ["reki.stream"] = "product" });
            LogSent(
                "notification_sent",
                notification.PlayerId.Value,
                notification.Id.Value,
                channelType.ToString().ToLowerInvariant(),
                "delivery",
                timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        }
        catch (Exception error)
        {
            LogSendError(error, notification.PlayerId.Value, notification.Id.Value, channelType);
        }
    }

    protected abstract Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification accepted by provider: {event_name}, player {player_id:N}, notification {prop_id:N}, channel {prop_kind}, activity {activity_kind}, time {event_time_unix_ms}")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private partial void LogSent(string event_name, Guid player_id, Guid prop_id, string prop_kind, string activity_kind, long event_time_unix_ms);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification {NotificationId:N} delivery failed for player {PlayerId:N} via {ChannelType}")]
    private partial void LogSendError(Exception ex, Guid playerId, Guid notificationId, NotificationChannelType channelType);
}
