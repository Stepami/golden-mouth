using Mediator;
using Reki.Application.Common;

namespace Reki.Application.Telemetry;

public sealed class ReportCommentCommandHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<ReportComment>
{
    public async ValueTask<Unit> Handle(
        ReportComment request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        telemetry.Track(player.Id, "report", new Dictionary<string, string>());
        return Unit.Value;
    }
}

