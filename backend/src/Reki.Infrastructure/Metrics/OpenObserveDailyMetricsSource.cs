using System.Globalization;
using Microsoft.Extensions.Options;
using Reki.Application.Exceptions;
using Reki.Application.Metrics.GetDailyMetrics;
using Reki.Application.Metrics.Responses;

namespace Reki.Infrastructure.Metrics;

internal sealed class OpenObserveDailyMetricsSource(
    IOpenObserveMetricsApi api,
    IOptionsMonitor<DailyMetricsOptions> options,
    TimeProvider timeProvider) : IDailyMetricsSource
{
    public async Task<IReadOnlyList<DailyMetricResponse>> GetAsync(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (settings.HistoryFrom is not { } historyFrom || dateFrom < historyFrom ||
            settings.InitialTotalUsers is not { } initialTotal || !settings.ExclusionsConfirmed || !settings.CoverageConfirmed ||
            string.IsNullOrWhiteSpace(settings.Authorization) || string.IsNullOrWhiteSpace(settings.BaseUrl) ||
            string.IsNullOrWhiteSpace(settings.Organization))
        {
            throw new MetricsUnavailableException();
        }

        var now = timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(3));
        var today = DateOnly.FromDateTime(now.DateTime);
        if (dateTo > today)
        {
            throw new MetricsUnavailableException();
        }

        var start = new DateTimeOffset(historyFrom.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3));
        var end = new DateTimeOffset(dateTo.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(3)).AddDays(1);
        var exclusions = settings.ExcludedPlayerIds.Length == 0
            ? string.Empty
            : " AND player_id NOT IN (" + string.Join(",", settings.ExcludedPlayerIds.Select(id => $"'{id:N}'")) + ")";
        var sql = string.Create(CultureInfo.InvariantCulture, $"""
            WITH source AS (
              SELECT event_time_unix_ms, event_name, player_id, prop_id
              FROM "product"
              WHERE event_time_unix_ms >= {start.AddDays(-1).ToUnixTimeMilliseconds()} AND event_time_unix_ms < {end.ToUnixTimeMilliseconds()}
                AND player_id IS NOT NULL AND player_id != ''{exclusions}
            ), logical_llm AS (
              SELECT player_id, prop_id, MIN(event_time_unix_ms) AS event_time_unix_ms
              FROM source WHERE event_name = 'llm_requested' AND prop_id IS NOT NULL AND prop_id != ''
              GROUP BY player_id, prop_id
            ), daily AS (
              SELECT CAST(floor((event_time_unix_ms + 10800000) / 86400000) AS BIGINT) AS day_number,
                COUNT(DISTINCT CASE WHEN event_name IN ('post_scored','comment_created','world_created') THEN player_id END) AS dau,
                COUNT(DISTINCT CASE WHEN event_name = 'player_created' THEN player_id END) AS new_users,
                0 AS llm_requests
              FROM source WHERE event_time_unix_ms >= {start.ToUnixTimeMilliseconds()}
              GROUP BY day_number
              UNION ALL
              SELECT CAST(floor((event_time_unix_ms + 10800000) / 86400000) AS BIGINT) AS day_number,
                0 AS dau, 0 AS new_users, COUNT(*) AS llm_requests
              FROM logical_llm WHERE event_time_unix_ms >= {start.ToUnixTimeMilliseconds()}
              GROUP BY day_number
            )
            SELECT day_number, SUM(dau) AS dau, SUM(new_users) AS new_users, SUM(llm_requests) AS llm_requests
            FROM daily GROUP BY day_number ORDER BY day_number
            """);
        var query = new OpenObserveMetricsQuery(
            sql,
            start.AddDays(-1).ToUnixTimeMilliseconds() * 1000,
            end.ToUnixTimeMilliseconds() * 1000,
            0,
            dateTo.DayNumber - historyFrom.DayNumber + 1);
        var response = await api.SearchAsync(
            settings.Organization,
            settings.Authorization,
            new OpenObserveMetricsRequest(query, 8),
            cancellationToken);
        if (response.IsPartial)
        {
            throw new MetricsUnavailableException();
        }

        var epoch = new DateOnly(1970, 1, 1).DayNumber;
        var counts = response.Hits.ToDictionary(hit => checked(epoch + hit.GetProperty("day_number").GetInt32()));
        var total = initialTotal;
        var result = new List<DailyMetricResponse>();
        for (var index = historyFrom.DayNumber; index <= dateTo.DayNumber; index++)
        {
            var exists = counts.TryGetValue(index, out var hit);
            var newUsers = exists ? hit.GetProperty("new_users").GetInt64() : 0;
            total += newUsers;
            if (index >= dateFrom.DayNumber)
            {
                // The application never executes model tools; FunctionCallContent is rejected.
                result.Add(new DailyMetricResponse(
                    DateOnly.FromDayNumber(index),
                    exists ? hit.GetProperty("dau").GetInt64() : 0,
                    exists ? hit.GetProperty("llm_requests").GetInt64() : 0,
                    0,
                    total,
                    newUsers,
                    index < today.DayNumber));
            }
        }

        return result;
    }
}
