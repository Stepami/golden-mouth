using Mediator;

namespace Reki.Application.Events;

public sealed record AnswerEventCommand(long TelegramUserId, string EventId, string Text) : IRequest;

