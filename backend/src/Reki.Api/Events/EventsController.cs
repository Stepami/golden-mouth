using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Events.AnswerEventCommand;
using Reki.Application.Events.GetTodayEvent;
using Reki.Contracts.Events;
using DomainEventId = Reki.Domain.Events.EventId;

namespace Reki.Api.Events;

[ApiController]
[Produces("application/json")]
[Route("events")]
public sealed class EventsController(
    ISender sender,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("today", Name = "GetTodayEvent")]
    [ActionName("GetTodayEvent")]
    public async Task<Ok<GameEventResponse>> GetTodayEventAsync(
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var gameEvent = await sender.Send(
            new TodayEventQuery(Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapEvent(gameEvent));
    }

    [HttpPost("{eventId}/answer", Name = "AnswerEvent")]
    [ActionName("AnswerEvent")]
    public async Task<Ok> AnswerEventAsync(
        string eventId,
        AnswerEventRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AnswerEvent(
                DomainEventId.From(eventId),
                request.Text,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok();
    }
}