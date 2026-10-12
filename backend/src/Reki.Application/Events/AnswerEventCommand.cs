using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Application.Events;

public sealed record AnswerEventCommand(EventId EventId, string Text, WorldId WorldId) : IRequest;