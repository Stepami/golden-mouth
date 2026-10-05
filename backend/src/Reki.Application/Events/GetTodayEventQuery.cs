using Mediator;
using Reki.Domain.Events;

namespace Reki.Application.Events;

public sealed record GetTodayEventQuery(long TelegramUserId) : IRequest<DailyGameEvent>;

