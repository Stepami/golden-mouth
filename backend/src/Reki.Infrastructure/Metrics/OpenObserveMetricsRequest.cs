namespace Reki.Infrastructure.Metrics;

public sealed record OpenObserveMetricsRequest(OpenObserveMetricsQuery Query, int Timeout);
