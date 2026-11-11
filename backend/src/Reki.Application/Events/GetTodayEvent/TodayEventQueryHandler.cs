using Mediator;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;
using Reki.Domain.Events;

namespace Reki.Application.Events.GetTodayEvent;

internal sealed class TodayEventQueryHandler(
    IDailyGameEventRepository dailyGameEventRepository,
    GameDayPeriod gameDayPeriod,
    IRekiContextAccessor rekiContextAccessor) : IQueryHandler<TodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        TodayEventQuery query,
        CancellationToken cancellationToken)
    {
        var playerId = rekiContextAccessor.Context.PlayerId;
        var world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(query.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var periodStart = gameDayPeriod.PeriodStart;
        var existing = await dailyGameEventRepository.GetTodayEventAsync(
            playerId,
            periodStart,
            cancellationToken,
            world.Id);
        if (existing is not null)
        {
            return existing;
        }

        var gameEvent = new DailyGameEvent(
            RekiEventId.New(),
            playerId,
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
