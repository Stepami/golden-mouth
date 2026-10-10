using Mediator;
using Reki.Domain.Events;

namespace Reki.Application.Events;

public sealed record AnswerEventCommand(EventId EventId, string Text) : IRequest;

