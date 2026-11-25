using Mediator;
using Reki.Application.Notifications;
using Reki.Application.Player;
using Reki.Application.Player.RefreshDailyChallengeCommand;
using Reki.Domain.Notifications;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RefreshDailyChallengesJob(
    IPlayerRepository players,
    ISender sender,
    INotificationRepository notificationRepository,
    INotificationContentFactory<DateOnly> challengeContentFactory,
    TimeProvider timeProvider) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var now = TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
        var since = new DateTimeOffset(now.Date.AddDays(-3), now.Offset).ToUniversalTime();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var content = await challengeContentFactory.GetContent(today);
        await foreach (var player in players.GetActivePlayersAsync(since, today, cancellationToken))
        {
            await sender.Send(new RefreshDailyChallenge(player), cancellationToken);
            await Task.WhenAll(
                player.NotificationChannels.Keys.Select(channelType => notificationRepository.SaveAsync(
                    Notification.New(
                        player.Id,
                        channelType,
                        now.AddHours(2).AddMinutes(Random.Shared.Next(-9, 10)),
                        content,
                        NotificationKind.Challenge),
                    cancellationToken).AsTask()));
        }
    }
}
