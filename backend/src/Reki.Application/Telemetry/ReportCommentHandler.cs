using Mediator;
using Reki.Application.Player;

namespace Reki.Application.Telemetry;

public sealed class ReportCommentHandler(
    ProductTelemetry telemetry,
    ISender sender)
    : IRequestHandler<ReportCommentCommand>
{
    public async ValueTask<Unit> Handle(
        ReportCommentCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        telemetry.Track(player.Id, "report", new Dictionary<string, string>());
        return Unit.Value;
    }
}

