using Mediator;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Domain.Events;

namespace Reki.Application.Events;

public sealed class GetTodayEventHandler(
    IGameStore store,
    GameDayPeriod gameDayPeriod,
    ISender sender) : IRequestHandler<GetTodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        GetTodayEventQuery request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(player.Id, request.WorldId), cancellationToken);
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