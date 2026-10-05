using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

public sealed class ReportCommentHandler(
    ProductTelemetry telemetry,
    PlayerReader playerReader)
    : IRequestHandler<ReportCommentCommand>
{
    public async ValueTask<Unit> Handle(
        ReportCommentCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        telemetry.Track(player.Id, "report", new Dictionary<string, string>());
        return Unit.Value;
    }
}

