using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Reki.Domain.Notifications;

namespace Reki.Application.Notifications;

public abstract partial class NotificationSender(
    NotificationChannelType channelType,
    ILogger<NotificationSender> logger,
    TimeProvider timeProvider) : INotificationSender
{
    public Task<NotificationStatus> SendAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct) =>
        channel.Type == channelType ? SendInternalAsync(channel, notification, ct) : Task.FromResult(NotificationStatus.Pending);

    protected abstract Task<NotificationStatus> SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct);

    protected async Task<NotificationStatus> SendDestinationAsync(Notification notification, Func<Task> sendAsync, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            await sendAsync();
        }
        catch (Exception error)
        {
            ct.ThrowIfCancellationRequested();
            LogSendError(error, notification.PlayerId.Value, notification.Id.Value, channelType);
            return NotificationStatus.Failed;
        }

        // Only the routing tag belongs in the scope; event data comes from the message template.
        using var scope = logger.BeginScope(new Dictionary<string, object?> { ["reki.stream"] = "product" });
        LogSent(
            "notification_sent",
            notification.PlayerId.Value,
            notification.Id.Value,
            channelType.ToString().ToLowerInvariant(),
            "delivery",
            timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
        return NotificationStatus.Sent;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification accepted by provider: {event_name}, player {player_id:N}, notification {prop_id:N}, channel {prop_kind}, activity {activity_kind}, time {event_time_unix_ms}")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private partial void LogSent(string event_name, Guid player_id, Guid prop_id, string prop_kind, string activity_kind, long event_time_unix_ms);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification {NotificationId:N} delivery failed for player {PlayerId:N} via {ChannelType}")]
    private partial void LogSendError(Exception ex, Guid playerId, Guid notificationId, NotificationChannelType channelType);
}
