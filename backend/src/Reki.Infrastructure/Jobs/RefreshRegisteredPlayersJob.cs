using Reki.Infrastructure.Persistence;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed class RefreshRegisteredPlayersJob(RegisteredPlayersMetrics metrics) : ITickerFunction
{
    public Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default) =>
        metrics.RefreshAsync(cancellationToken);
}
