using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Telemetry;

public sealed class ReportCommentHandler(
    ITelegramUserAccessor telegramUser,
    ProductTelemetry telemetry,
    PlayerReader playerReader)
    : IRequestHandler<ReportCommentCommand>
{
    public async ValueTask<Unit> Handle(
        ReportCommentCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        telemetry.Track(player.Id, "report", new Dictionary<string, string>());
        return Unit.Value;
    }
}

