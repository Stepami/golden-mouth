using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;
using Reki.Domain.Game.Telemetry;

namespace Reki.Application.Game.Telemetry;

public sealed class ReportCommentHandler(
    IGameStore store,
    TimeProvider timeProvider,
    PlayerReader playerReader)
    : IRequestHandler<ReportCommentCommand>
{
    public async ValueTask<Unit> Handle(
        ReportCommentCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        long timestamp = now.ToUnixTimeMilliseconds();
        var properties = new Dictionary<string, string>
        {
            ["comment_id"] = request.CommentId,
            ["reason"] = request.Reason,
        };
        var analyticsEvent = new AnalyticsEntry(
            "report",
            timestamp,
            "report",
            null,
            properties);
        string sessionId = Guid.NewGuid().ToString("N");
        var experiments = new Dictionary<string, string>();
        var batch = new AnalyticsBatch(
            player.Id,
            sessionId,
            "server",
            experiments,
            [analyticsEvent],
            now);
        await store.AppendAnalyticsAsync(batch, cancellationToken);
        return Unit.Value;
    }
}

