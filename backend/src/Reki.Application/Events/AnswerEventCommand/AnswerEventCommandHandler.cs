using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;

namespace Reki.Application.Events.AnswerEventCommand;

internal sealed class AnswerEventCommandHandler(
    IDailyGameEventRepository dailyGameEventRepository,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<AnswerEvent>
{
    public async ValueTask<Unit> Handle(
        AnswerEvent request,
        CancellationToken cancellationToken)
    {
        var playerId = rekiContextAccessor.Context.PlayerId;
        var world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var gameEvent = await dailyGameEventRepository.GetTodayEventAsync(
            playerId,
            DateTimeOffset.MinValue,
            cancellationToken,
            world.Id);
        if (gameEvent is null || gameEvent.Id != request.RekiEventId)
        {
            throw new ResourceNotFoundException("Event not found.");
        }

        await dailyGameEventRepository.SaveEventAsync(gameEvent with { Answer = request.Text }, cancellationToken);
        return Unit.Value;
    }
}
