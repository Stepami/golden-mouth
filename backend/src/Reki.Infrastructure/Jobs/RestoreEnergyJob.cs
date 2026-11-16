using Microsoft.Extensions.Options;
using Reki.Application;
using Reki.Application.Balance;
using Reki.Application.Player;
using Reki.Domain.Player;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RestoreEnergyJob(IUnitOfWork unitOfWork, IPlayerRepository players, IOptionsMonitor<GameBalance> balance, TimeProvider timeProvider) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await foreach (var player in players.GetPlayersForEnergyRecoveryAsync(balance.CurrentValue.EnergyCap, now.AddSeconds(-PlayerWallet.RegenSeconds), cancellationToken))
        {
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    var current = await players.GetPlayerAsync(player.Id, token);
                    if (current is null)
                    {
                        return false;
                    }

                    var previousVersion = current.Version;
                    current.RestoreEnergy(now);
                    if (current.Version != previousVersion)
                    {
                        await players.ReplacePlayerAsync(current, token);
                    }

                    return true;
                },
                cancellationToken);
        }
    }
}
