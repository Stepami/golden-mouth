using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Application.Events.AnswerEventCommand;

public sealed record AnswerEvent(RekiEventId RekiEventId, string Text, WorldId WorldId) : ICommand;