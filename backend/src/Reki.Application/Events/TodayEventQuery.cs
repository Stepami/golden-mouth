using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Application.Events;

public sealed record TodayEventQuery(WorldId WorldId) : IRequest<DailyGameEvent>;