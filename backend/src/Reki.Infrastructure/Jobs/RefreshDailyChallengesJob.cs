using Mediator;
using Reki.Application.Player;
using Reki.Application.Player.RefreshDailyChallengeCommand;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RefreshDailyChallengesJob(
    IPlayerRepository players,
    ISender sender,
    TimeProvider timeProvider) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var now = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
        var since = new DateTimeOffset(now.Date.AddDays(-3), now.Offset).ToUniversalTime();
        await foreach (var player in players.GetActivePlayersAsync(since, cancellationToken))
        {
            await sender.Send(new RefreshDailyChallenge(player), cancellationToken);
        }
    }
}
