using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetTodayEventQuery(long TelegramUserId) : IRequest<DailyGameEvent>;

