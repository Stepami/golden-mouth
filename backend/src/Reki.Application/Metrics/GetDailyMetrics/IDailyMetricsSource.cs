using Reki.Application.Metrics.Responses;

namespace Reki.Application.Metrics.GetDailyMetrics;

public interface IDailyMetricsSource
{
    Task<IReadOnlyList<DailyMetricResponse>> GetAsync(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken);
}
