using Mediator;
using Reki.Domain.Game.Events;

namespace Reki.Application.Game.Events;

public sealed record GetTodayEventQuery(long TelegramUserId) : IRequest<DailyGameEvent>;

