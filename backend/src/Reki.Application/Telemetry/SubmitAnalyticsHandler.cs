using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    ProductTelemetry telemetry,
    ISender sender)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        telemetry.TrackSubmission(player, request.Submission);
        return Unit.Value;
    }
}

