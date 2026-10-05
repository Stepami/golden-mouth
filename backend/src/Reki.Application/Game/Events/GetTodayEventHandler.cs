using Mediator;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed class GetTodayEventHandler(
    IGameStore store,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetTodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        GetTodayEventQuery request,
        CancellationToken cancellationToken)
    {
        PlayerProfile player = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
        DateTimeOffset periodStart = gameDay.PeriodStart;
        DailyGameEvent? existing = await store.GetTodayEventAsync(
            player.Id,
            periodStart,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        string eventId = Guid.NewGuid().ToString("N");
        var gameEvent = new DailyGameEvent(
            eventId,
            player.Id,
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

