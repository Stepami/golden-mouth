using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Notifications;
using Reki.Domain.Notifications;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Reki.Infrastructure.Telegram;

internal sealed class TelegramNotificationSender(
    ITelegramBotClient telegram,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramNotificationSender> logger,
    TimeProvider timeProvider)
    : NotificationSender(NotificationChannelType.Telegram, logger, timeProvider)
{
    protected override Task SendInternalAsync(PlayerNotificationChannel channel, Notification notification, CancellationToken ct) =>
        telegram.SendMessage(
            ((TelegramNotificationChannel)channel).ChatId,
            notification.Content,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(
                "Запустить MiniApp",
                telegramOptions.Value.MiniAppUrl)),
            cancellationToken: ct);
}
