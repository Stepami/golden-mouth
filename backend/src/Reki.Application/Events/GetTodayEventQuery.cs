using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Player;

namespace Reki.Application.Events;

public sealed record GetTodayEventQuery(TelegramUserId TelegramUserId) : IRequest<DailyGameEvent>;

