using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using Reki.Domain.Telemetry;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    IGameStore store,
    TimeProvider timeProvider,
    PlayerReader playerReader)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        AnalyticsSubmission submission = request.Submission;
        await store.AppendAnalyticsAsync(
            new AnalyticsBatch(
                player.Id,
                submission.Session,
                submission.Platform,
                submission.Experiments,
                submission.Events,
                timeProvider.GetUtcNow()),
            cancellationToken);
        return Unit.Value;
    }
}

