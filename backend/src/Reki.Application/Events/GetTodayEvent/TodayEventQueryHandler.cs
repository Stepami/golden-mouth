using Mediator;
using Reki.Application.Common;
using Reki.Application.Exceptions;
using Reki.Domain.Events;

namespace Reki.Application.Events.GetTodayEvent;

public sealed class TodayEventQueryHandler(
    IDailyGameEventRepository dailyGameEventRepository,
    GameDayPeriod gameDayPeriod,
    IRekiContextAccessor rekiContextAccessor) : IQueryHandler<TodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        TodayEventQuery request,
        CancellationToken cancellationToken)
    {
        var player = await rekiContextAccessor.Context.Player;
        var world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var periodStart = gameDayPeriod.PeriodStart;
        var existing = await dailyGameEventRepository.GetTodayEventAsync(
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
        await dailyGameEventRepository.SaveEventAsync(gameEvent, cancellationToken);
        return gameEvent;
    }
}
