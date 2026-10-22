using Mediator;
using Reki.Application.Common;
using Reki.Application.Exceptions;

namespace Reki.Application.Events.AnswerEventCommand;

public sealed class AnswerEventCommandHandler(
    IDailyGameEventRepository dailyGameEventRepository,
    IRekiContextAccessor rekiContextAccessor)
    : ICommandHandler<AnswerEvent>
{
    public async ValueTask<Unit> Handle(
        AnswerEvent request,
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
