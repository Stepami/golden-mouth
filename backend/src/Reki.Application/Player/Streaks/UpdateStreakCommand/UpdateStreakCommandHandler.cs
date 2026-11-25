using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Reki.Application.Notifications;
using Reki.Application.Player.Streaks.GetStreak;
using Reki.Domain.Notifications;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.UpdateStreakCommand;

internal sealed class UpdateStreakCommandHandler(
    ISender sender,
    IPlayerStreakRepository streaks,
    IPlayerRepository players,
    INotificationRepository notifications,
    INotificationContentFactory<PlayerStreak> streakContentFactory,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    TimeProvider timeProvider) : ICommandHandler<UpdateStreak>
{
    public async ValueTask<Unit> Handle(UpdateStreak command, CancellationToken cancellationToken)
    {
        var streak = await sender.Send(new PlayerStreakQuery(command.Player.Id), cancellationToken);
        var now = timeProvider.GetUtcNow();

        var content = await streakContentFactory.GetContent(streak);
        var progress = streak.Progress(now);
        if (progress == PlayerStreakProgress.None)
        {
            return Unit.Value;
        }

        var award = streak.ShouldAward();
        if (award)
        {
            command.Player.ReplenishCrystals(1);
        }

        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                await streaks.SaveAsync(streak, token);
                if (progress == PlayerStreakProgress.Stopped)
                {
                    var newStreak = PlayerStreak.New(command.Player.Id, now);
                    newStreak.Progress(now);
                    await streaks.SaveAsync(newStreak, token);
                }
                else if (award)
                {
                    await players.ReplacePlayerAsync(command.Player, token);
                }

                var sendAt = new DateTimeOffset(now.Date.AddDays(1).AddHours(20), TimeSpan.FromHours(3));
                foreach (var channelType in command.Player.NotificationChannels.Keys)
                {
                    await notifications.SaveAsync(
                        Notification.New(
                            command.Player.Id,
                            channelType,
                            sendAt,
                            content,
                            NotificationKind.Streak),
                        token);
                }

                return Unit.Value;
            },
            _ => cache.Remove(CacheKeyPrefixes.PlayerStreak + command.Player.Id),
            cancellationToken);
    }
}
