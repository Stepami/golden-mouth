using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Worlds;
using Reki.Domain.Player;

namespace Reki.Application.Player.RefreshDailyChallengeCommand;

internal sealed class RefreshDailyChallengeCommandHandler(
    IPlayerRepository players,
    IWorldPlayerCache worlds,
    IUnitOfWork unitOfWork,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider) : ICommandHandler<RefreshDailyChallenge, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(RefreshDailyChallenge command, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (command.Player.DailyChallenge.Day == day)
        {
            return command.Player;
        }

        var playerId = command.Player.Id;
        var playerWorlds = await worlds.GetWorldsAsync(playerId, cancellationToken);
        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await players.GetPlayerAsync(playerId, token)
                             ?? throw new ResourceNotFoundException("Player not found.");
                if (player.DailyChallenge.Day < day)
                {
                    var previousVersion = player.Version;
                    player.RefreshDailyChallenge(
                        day,
                        [.. playerWorlds.Values],
                        balance.CurrentValue.DailyChallengeTargets[day.DayOfWeek]);
                    await players.SaveDailyChallengeAsync(player, previousVersion, token);
                }

                return player;
            },
            cancellationToken);
    }
}
