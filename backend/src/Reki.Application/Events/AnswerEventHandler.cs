using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Events;

public sealed class AnswerEventHandler(
    IDailyGameEventRepository dailyGameEventRepository,
    IRekiContextAccessor rekiContextAccessor)
    : IRequestHandler<AnswerEventCommand>
{
    public async ValueTask<Unit> Handle(
        AnswerEventCommand request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var gameEvent = await dailyGameEventRepository.GetTodayEventAsync(
            player.Id,
            DateTimeOffset.MinValue,
            cancellationToken,
            world.Id);
        if (gameEvent is null || gameEvent.Id != request.EventId)
        {
            throw new ResourceNotFoundException("Event not found.");
        }

        await dailyGameEventRepository.SaveEventAsync(gameEvent with { Answer = request.Text }, cancellationToken);
        return Unit.Value;
    }
}
