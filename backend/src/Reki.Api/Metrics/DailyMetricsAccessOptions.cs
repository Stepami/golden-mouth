namespace Reki.Api.Metrics;

internal sealed class DailyMetricsAccessOptions
{
    public const string SectionName = "DailyMetricsAccess";

    public string Token { get; init; } = string.Empty;
}
