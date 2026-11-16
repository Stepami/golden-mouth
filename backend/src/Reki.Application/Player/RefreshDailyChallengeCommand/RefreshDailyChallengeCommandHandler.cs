using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Worlds;

namespace Reki.Application.Player.RefreshDailyChallengeCommand;

internal sealed class RefreshDailyChallengeCommandHandler(
    IPlayerRepository players,
    IWorldRepository worlds,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider) : ICommandHandler<RefreshDailyChallenge>
{
    public async ValueTask<Unit> Handle(RefreshDailyChallenge command, CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
        var day = DateOnly.FromDateTime(localNow.AddHours(-9).DateTime);
        var player = command.Player;
        if (player.DailyChallenge.Day == day)
        {
            return Unit.Value;
        }

        var playerWorlds = await worlds.GetWorldsAsync(player.Id, cancellationToken);
        var previousVersion = player.Version;
        player.RefreshDailyChallenge(day, playerWorlds, balance.CurrentValue.DailyChallengeTargets[day.DayOfWeek]);
        await players.SaveDailyChallengeAsync(player, previousVersion, cancellationToken);
        return Unit.Value;
    }
}
