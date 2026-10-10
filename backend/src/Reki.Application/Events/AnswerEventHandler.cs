using Mediator;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Domain.Events;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Application.Events;

public sealed class AnswerEventHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store, PlayerReader playerReader)
    : IRequestHandler<AnswerEventCommand>
{
    public async ValueTask<Unit> Handle(
        AnswerEventCommand request,
        CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        PlayerProfile player = await playerReader.GetAsync(telegramUserId, cancellationToken);
        DailyGameEvent? gameEvent = await store.GetTodayEventAsync(
            player.Id,
            DateTimeOffset.MinValue,
            cancellationToken);
        if (gameEvent is null || gameEvent.Id != request.EventId)
        {
            throw new ResourceNotFoundException("Event not found.");
        }

        await store.SaveEventAsync(gameEvent with { Answer = request.Text }, cancellationToken);
        return Unit.Value;
    }
}

