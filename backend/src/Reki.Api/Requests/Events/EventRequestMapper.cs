using Reki.Application.Events.AnswerEventCommand;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Events;

internal static class EventRequestMapper
{
    public static AnswerEvent ToAnswerEvent(this AnswerEventRequest source, RekiEventId eventId, WorldId worldId) =>
        new(eventId, source.Text, worldId);
}
