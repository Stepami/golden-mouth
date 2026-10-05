using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed class ServerAnalyticsWriter(IGameStore store, TimeProvider timeProvider)
{
    public async ValueTask TrackAsync(
        PlayerId playerId,
        string eventName,
        IReadOnlyDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        var entry = new AnalyticsEntry(
            eventName,
            now.ToUnixTimeMilliseconds(),
            "server",
            null,
            properties);
        await store.AppendAnalyticsAsync(
            new AnalyticsBatch(
                playerId,
                SessionId.New(),
                "server",
                new Dictionary<string, string>(),
                [entry],
                now),
            cancellationToken);
    }
}
