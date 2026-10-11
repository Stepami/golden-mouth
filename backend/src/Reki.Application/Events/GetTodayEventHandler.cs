using Mediator;
using Reki.Application.Common;
using Reki.Domain.Events;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Events;

public sealed class GetTodayEventHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameDay gameDay,
    PlayerReader playerReader) : IRequestHandler<GetTodayEventQuery, DailyGameEvent>
{
    public async ValueTask<DailyGameEvent> Handle(
        GetTodayEventQuery request,
        CancellationToken cancellationToken)
    {
        var telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        var player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        var periodStart = gameDay.PeriodStart;
        var existing = await store.GetTodayEventAsync(
            player.Id,
            periodStart,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var gameEvent = new DailyGameEvent(
            EventId.New(),
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

