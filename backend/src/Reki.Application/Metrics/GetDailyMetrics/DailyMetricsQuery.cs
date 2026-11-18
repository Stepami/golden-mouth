using Mediator;
using Reki.Application.Metrics.Responses;

namespace Reki.Application.Metrics.GetDailyMetrics;

public sealed record DailyMetricsQuery(DateOnly DateFrom, DateOnly DateTo) : IQuery<DailyMetricsResponse>;
