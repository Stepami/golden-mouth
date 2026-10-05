using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Events;
using Reki.Domain.Player;

namespace Reki.Application.Events;

public sealed class AnswerEventHandler(IGameStore store, PlayerReader playerReader)
    : IRequestHandler<AnswerEventCommand>
{
    public async ValueTask<Unit> Handle(
        AnswerEventCommand request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DailyGameEvent? gameEvent = await store.GetTodayEventAsync(
            player.Id,
            DateTimeOffset.MinValue,
            cancellationToken);
        if (gameEvent is null || !string.Equals(gameEvent.Id, request.EventId, StringComparison.Ordinal))
        {
            throw new ResourceNotFoundException("Event not found.");
        }

        await store.SaveEventAsync(gameEvent with { Answer = request.Text }, cancellationToken);
        return Unit.Value;
    }
}

