using Microsoft.Extensions.Logging;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Telegram.Bot;

namespace Reki.Infrastructure.Telegram;

internal sealed class TelegramNotificationSender(
    ITelegramBotClient telegram,
    ILogger<TelegramNotificationSender> logger,
    TimeProvider timeProvider)
    : NotificationSender(NotificationChannelType.Telegram, logger, timeProvider)
{
    protected override Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct) =>
        telegram.SendMessage(((TelegramNotificationChannel)channel).ChatId, notification.Content, cancellationToken: ct);
}
