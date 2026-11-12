using System.Globalization;
using Microsoft.Extensions.Logging;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed partial class TelegramNotificationSender(
    ITelegramBotClient telegram,
    ILogger<TelegramNotificationSender> logger)
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
        }
        catch (Exception error)
        {
            LogSendError(error);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Notification delivery failed for channel Telegram")]
    private partial void LogSendError(Exception ex);
}