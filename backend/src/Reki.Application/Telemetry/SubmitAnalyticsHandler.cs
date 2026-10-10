using Mediator;
using Reki.Application.Common;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Telemetry;

public sealed class SubmitAnalyticsHandler(
    ITelegramUserAccessor telegramUser,
    ProductTelemetry telemetry,
    PlayerReader playerReader)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        telemetry.TrackSubmission(player, request.Submission);
        return Unit.Value;
    }
}

