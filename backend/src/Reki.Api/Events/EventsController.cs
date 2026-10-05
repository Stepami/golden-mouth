using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Events;
using Reki.Contracts.Events;
using Reki.Domain.Events;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Events;

[ApiController]
[Produces("application/json")]
[Route("events")]
public sealed class EventsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("today", Name = "GetTodayEvent")]
    [ActionName("GetTodayEvent")]
    public async Task<Ok<GameEventResponse>> GetTodayEventAsync(
        CancellationToken cancellationToken)
    {
        DailyGameEvent gameEvent = await sender.Send(
            new GetTodayEventQuery(telegramUser.User.Id),
            cancellationToken);
        return TypedResults.Ok(mapper.MapEvent(gameEvent));
    }

    [HttpPost("{eventId}/answer", Name = "AnswerEvent")]
    [ActionName("AnswerEvent")]
    public async Task<Ok> AnswerEventAsync(
        string eventId,
        AnswerEventRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AnswerEventCommand(telegramUser.User.Id, eventId, request.Text),
            cancellationToken);
        return TypedResults.Ok();
    }
}
