using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

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

