using Reki.Application.Player;
using Reki.Domain.Player;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

internal sealed class RestoreEnergyJob(IPlayerRepository players, TimeProvider timeProvider) : ITickerFunction
{
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await foreach (var player in players.GetPlayersForEnergyRecoveryAsync(now.AddSeconds(-PlayerWallet.RegenSeconds), cancellationToken))
        {
            var wallet = player.Wallet.Restore(now);
            await players.TrySaveEnergyRecoveryAsync(player, wallet, cancellationToken);
        }
    }
}
