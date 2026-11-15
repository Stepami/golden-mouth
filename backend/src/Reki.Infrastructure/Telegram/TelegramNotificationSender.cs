using System.Globalization;
using Microsoft.Extensions.Logging;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed class TelegramNotificationSender(
    ITelegramBotClient telegram,
    ILogger<TelegramNotificationSender> logger,
    TimeProvider timeProvider)
    : NotificationSender(NotificationChannelType.Telegram, logger, timeProvider)
{
    protected override async Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct)
    {
        await telegram.SendMessage(
            long.Parse(channel.Properties["chatId"], CultureInfo.InvariantCulture),
            notification.Content,
            cancellationToken: ct);
    }
}
