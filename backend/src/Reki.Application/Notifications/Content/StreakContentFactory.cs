using Reki.Domain.Player.Streaks;

namespace Reki.Application.Notifications.Content;

internal sealed class StreakContentFactory(
    TimeProvider timeProvider) :
    INotificationContentFactory<PlayerStreak>
{
    public Task<string> GetContent(PlayerStreak context)
    {
        var now = timeProvider.GetUtcNow();
        PlayerStreak streakCopy = new(
            context.Id,
            context.PlayerId,
            context.StartedAt,
            context.Count,
            context.IsActive);
        var today = streakCopy.Progress(now);
        if (today is PlayerStreakProgress.Started or PlayerStreakProgress.Stopped)
        {
            return Task.FromResult(
                "Вчера началась твоя серия в РЕКАХ 🔥\n" +
                "Заходи, чтобы не потерять счётчик!");
        }

        streakCopy.Progress(now.AddDays(1));
        return streakCopy.ShouldAward()
            ? Task.FromResult($"Твой бонус за {streakCopy.Count}-й день серии — 1 кристалл 💎 Заходи в РЕКИ!")
            : Task.FromResult(
                $"Поставь новый рекорд 🔥 {streakCopy.Count}-й день подряд в РЕКАХ!\n" +
                "Продолжай серию и получишь награду!");
    }
}