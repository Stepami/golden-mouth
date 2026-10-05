using Mediator;

namespace Reki.Application.Game.Events;

public sealed record AnswerEventCommand(long TelegramUserId, string EventId, string Text) : IRequest;

