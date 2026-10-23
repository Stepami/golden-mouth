using Mediator;
using Reki.Application.RekiContext;

namespace Reki.Application.Telemetry.ReportCommentCommand;

internal sealed class ReportCommentCommandHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<ReportComment>
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

