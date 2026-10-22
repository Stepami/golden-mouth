using Reki.Application.Events.AnswerEventCommand;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Events;

internal static class EventRequestMapper
{
    public static AnswerEvent ToCommand(this AnswerEventRequest source, string eventId, string worldId) =>
        new(RekiEventId.From(eventId), source.Text, WorldId.From(worldId));
}
