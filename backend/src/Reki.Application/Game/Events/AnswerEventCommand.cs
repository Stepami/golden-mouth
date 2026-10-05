using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record AnswerEventCommand(long TelegramUserId, string EventId, string Text) : IRequest;

