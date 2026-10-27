using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Application.Events.GetTodayEvent;

public sealed record TodayEventQuery(WorldId WorldId) : IQuery<DailyGameEvent>;