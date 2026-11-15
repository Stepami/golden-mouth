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
                notification.PlayerId.Value.ToString("N"),
                notification.Id.Value.ToString("N"),
                channelType.ToString().ToLowerInvariant(),
                timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
                "notification_sent",
                "delivery");
        }
        catch (Exception error)
        {
            LogSendError(error, notification.PlayerId.Value.ToString("N"), notification.Id.Value.ToString("N"), channelType);
        }
    }

    protected abstract Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification accepted by provider: {event_name}, player {player_id}, notification {prop_id}, channel {prop_kind}, activity {activity_kind}, time {event_time_unix_ms}")]
    private partial void LogSent(string player_id, string prop_id, string prop_kind, long event_time_unix_ms, string event_name, string activity_kind);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification {NotificationId} delivery failed for player {PlayerId} via {ChannelType}")]
    private partial void LogSendError(Exception ex, string playerId, string notificationId, NotificationChannelType channelType);
}
