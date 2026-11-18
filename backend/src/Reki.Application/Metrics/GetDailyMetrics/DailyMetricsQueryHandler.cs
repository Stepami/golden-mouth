using Mediator;
using Reki.Application.Metrics.Responses;

namespace Reki.Application.Metrics.GetDailyMetrics;

internal sealed class DailyMetricsQueryHandler(IDailyMetricsSource source, TimeProvider timeProvider)
    : IQueryHandler<DailyMetricsQuery, DailyMetricsResponse>
{
    public async ValueTask<DailyMetricsResponse> Handle(DailyMetricsQuery query, CancellationToken cancellationToken)
    {
        var data = await source.GetAsync(query.DateFrom, query.DateTo, cancellationToken);
        return new DailyMetricsResponse("29378", "Europe/Moscow", timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(3)), data);
    }
}
