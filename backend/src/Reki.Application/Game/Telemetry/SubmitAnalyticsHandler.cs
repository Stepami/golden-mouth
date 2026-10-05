using Mediator;
using Reki.Application.Game.Common;
using Reki.Domain.Game.Player;

namespace Reki.Application.Game.Telemetry;

public sealed class SubmitAnalyticsHandler(
    IGameStore store,
    TimeProvider timeProvider,
    PlayerReader playerReader)
    : IRequestHandler<SubmitAnalyticsCommand>
{
    public async ValueTask<Unit> Handle(
        SubmitAnalyticsCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        await store.AppendAnalyticsAsync(
            request.Batch with
            {
                PlayerId = player.Id,
                ReceivedAtUtc = timeProvider.GetUtcNow(),
            },
            cancellationToken);
        return Unit.Value;
    }
}

