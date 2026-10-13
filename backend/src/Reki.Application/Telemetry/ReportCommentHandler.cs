using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Telemetry;

public sealed class ReportCommentHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<ReportCommentCommand>
{
    public async ValueTask<Unit> Handle(
        ReportCommentCommand request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        telemetry.Track(player.Id, "report", new Dictionary<string, string>());
        return Unit.Value;
    }
}

