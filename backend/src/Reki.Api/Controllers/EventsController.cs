using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Auth;
using Reki.Api.Requests.Events;
using Reki.Application.Events;
using Reki.Application.Events.GetTodayEvent;
using Reki.Application.Events.Responses;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.AppJwtPolicy)]
[Produces("application/json")]
[Route("events")]
public sealed class EventsController(
    ISender sender) : ControllerBase
{
    [HttpGet("today", Name = "GetTodayEvent")]
    [ActionName("GetTodayEvent")]
    public async Task<Ok<GameEventResponse>> GetTodayEventAsync(
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var gameEvent = await sender.Send(
            new TodayEventQuery(worldId),
            cancellationToken);
        return TypedResults.Ok(gameEvent.ToResponse());
    }

    [HttpPost("{eventId}/answer", Name = "AnswerEvent")]
    [ActionName("AnswerEvent")]
    public async Task<Ok> AnswerEventAsync(
        RekiEventId eventId,
        AnswerEventRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToAnswerEvent(eventId, worldId),
            cancellationToken);
        return TypedResults.Ok();
    }
}
