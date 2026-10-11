using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Application.Events;

public sealed record GetTodayEventQuery(WorldId WorldId) : IRequest<DailyGameEvent>;