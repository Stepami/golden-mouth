namespace Reki.Infrastructure.Metrics;

internal sealed class DailyMetricsOptions
{
    public const string SectionName = "DailyMetrics";

    public string BaseUrl { get; init; } = string.Empty;

    public string Organization { get; init; } = string.Empty;

    public string Authorization { get; init; } = string.Empty;

    public DateOnly? HistoryFrom { get; init; }

    public long? InitialTotalUsers { get; init; }

    public bool ExclusionsConfirmed { get; init; }

    public bool CoverageConfirmed { get; init; }

    public Guid[] ExcludedPlayerIds { get; init; } = [];
}
