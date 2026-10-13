using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Events;

namespace Reki.Application.Events;

public sealed class GetTodayEventHandler(
    IGameStore store,
    GameDayPeriod gameDayPeriod,
    IRekiContextAccessor contextAccessor) : IRequestHandler<GetTodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        GetTodayEventQuery request,
        CancellationToken cancellationToken)
    {
        var rekiContext = contextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var periodStart = gameDayPeriod.PeriodStart;
        var existing = await store.GetTodayEventAsync(
            player.Id,
            periodStart,
            cancellationToken,
            world.Id);
        if (existing is not null)
        {
            return existing;
        }

        var gameEvent = new DailyGameEvent(
            EventId.New(),
            player.Id,
            world.Id,
            "collab",
            "событие дня",
            "Зовут в коллаб",
            "Нужен короткий пост в голосе твоего канала.",
            ["согласиться", "предложить свой формат"],
            "+отношения",
            "Ответ можно дать один раз.",
            periodStart,
            null);
        await store.SaveEventAsync(gameEvent, cancellationToken);
        return gameEvent;
    }
}