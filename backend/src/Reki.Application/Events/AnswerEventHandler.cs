using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;

namespace Reki.Application.Events;

public sealed class AnswerEventHandler(
    IGameStore store,
    IRekiContextAccessor contextAccessor)
    : IRequestHandler<AnswerEventCommand>
{
    public async ValueTask<Unit> Handle(
        AnswerEventCommand request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var gameEvent = await store.GetTodayEventAsync(
            player.Id,
            DateTimeOffset.MinValue,
            cancellationToken,
            world.Id);
        if (gameEvent is null || gameEvent.Id != request.EventId)
        {
            throw new ResourceNotFoundException("Event not found.");
        }

        await store.SaveEventAsync(gameEvent with { Answer = request.Text }, cancellationToken);
        return Unit.Value;
    }
}