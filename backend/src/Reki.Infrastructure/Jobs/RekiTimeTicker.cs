using TickerQ.Utilities.Entities;

namespace Reki.Infrastructure.Jobs;

internal sealed class RekiTimeTicker : TimeTickerEntity<RekiTimeTicker>
{
    public RekiTimeTicker()
    {
        Retries = 3;
        RetryIntervals = [30, 120, 600];
    }
}
