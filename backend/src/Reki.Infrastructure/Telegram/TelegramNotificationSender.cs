using System.Globalization;
using Microsoft.Extensions.Logging;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed partial class TelegramNotificationSender(
    ITelegramBotClient telegram,
    ILogger<TelegramNotificationSender> logger,
    TimeProvider timeProvider)
    : INotificationSender
{
    public async Task SendAsync(
        PlayerNotificationChannel channel,
        Notification notification,
        CancellationToken ct)
    {
        if (channel.Type is not NotificationChannelType.Telegram)
        {
            return;
        }

        try
        {
            await telegram.SendMessage(
                long.Parse(channel.Properties["chatId"], CultureInfo.InvariantCulture),
                notification.Content,
                cancellationToken: ct);

            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["reki.stream"] = "product",
                ["event_name"] = "notification_sent",
                ["activity_kind"] = "delivery",
                ["player_id"] = notification.PlayerId.Value.ToString("N"),
                ["prop_id"] = notification.Id.Value.ToString("N"),
                ["prop_kind"] = "telegram",
                ["event_time_unix_ms"] = timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
            });
            LogSent();
        }
        catch (Exception error)
        {
            LogSendError(error);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification accepted by provider")]
    private partial void LogSent();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification delivery failed for channel Telegram")]
    private partial void LogSendError(Exception ex);
}