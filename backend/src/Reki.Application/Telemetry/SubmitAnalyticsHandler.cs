using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    ProductTelemetry telemetry,
    PlayerReader playerReader)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        telemetry.TrackSubmission(player, request.Submission);
        return Unit.Value;
    }
}

