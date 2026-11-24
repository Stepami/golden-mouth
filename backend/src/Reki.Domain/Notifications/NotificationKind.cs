using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;

namespace Reki.Domain.Notifications;

/// <summary>
/// Тип уведомления <see cref="Notification"/>.<br/>
/// Определяет содержание <see cref="Notification.Content"/> и расписание отправки.
/// </summary>
public enum NotificationKind
{
    /// <summary>
    /// Реккурентное уведомление по <see cref="PlayerProfile.LastSeenAt"/>.
    /// </summary>
    Regular,

    /// <summary>
    /// Напоминание сохранить серию посещений <see cref="PlayerStreak"/>.<br/>
    /// Актуально когда <see cref="PlayerStreak.IsActive"/>.
    /// </summary>
    Streak,

    /// <summary>
    /// Нотификация при обновлении <see cref="PlayerProfile.DailyChallenge"/>.
    /// </summary>
    Challenge,
}