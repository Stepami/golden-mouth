using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Game.Common;
using Reki.Application.Game.Events;
using Reki.Contracts.Game.Common;
using Reki.Contracts.Game.Events;
using Reki.Domain.Game.Events;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Game.Events;

[ApiController]
[Produces("application/json")]
[Route("events")]
public sealed class EventsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    GameMapper mapper) : ControllerBase
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
    public async Task<Ok<OkResponse>> AnswerEventAsync(
        string eventId,
        AnswerEventRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AnswerEventCommand(telegramUser.User.Id, eventId, request.Text),
            cancellationToken);
        return TypedResults.Ok(new OkResponse(true));
    }
}
