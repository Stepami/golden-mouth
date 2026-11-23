namespace Reki.Domain.Notifications;

public sealed record TelegramNotificationChannel(long ChatId)
    : PlayerNotificationChannel(NotificationChannelType.Telegram);
