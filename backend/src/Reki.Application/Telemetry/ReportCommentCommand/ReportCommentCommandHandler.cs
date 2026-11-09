using Mediator;
using Reki.Application.RekiContext;

namespace Reki.Application.Telemetry.ReportCommentCommand;

internal sealed class ReportCommentCommandHandler(
    ProductTelemetry telemetry,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<ReportComment>
{
    public ValueTask<Unit> Handle(
        ReportComment request,
        CancellationToken cancellationToken)
    {
        var playerId = rekiContextAccessor.Context.PlayerId;
        telemetry.Track(playerId, "report", new Dictionary<string, string>());
        return ValueTask.FromResult(Unit.Value);
    }
}

