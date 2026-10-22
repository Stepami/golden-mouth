using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Events;
using Reki.Application.Events;
using Reki.Application.Events.AnswerEventCommand;
using Reki.Application.Events.GetTodayEvent;
using Reki.Application.Events.Responses;
using Reki.Domain.Events;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("events")]
public sealed class EventsController(
    ISender sender) : ControllerBase
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
        return TypedResults.Ok(gameEvent.ToResponse());
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
            request.ToCommand(eventId, worldId),
            cancellationToken);
        return TypedResults.Ok();
    }
}