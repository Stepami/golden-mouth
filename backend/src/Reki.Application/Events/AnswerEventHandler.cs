using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;

namespace Reki.Application.Events;

public sealed class AnswerEventHandler(
    IGameStore store,
    ISender sender)
    : IRequestHandler<AnswerEventCommand>
{
    public async ValueTask<Unit> Handle(
        AnswerEventCommand request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Reki.Application.Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
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