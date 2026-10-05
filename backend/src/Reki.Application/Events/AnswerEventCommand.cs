using Mediator;
using Reki.Domain.Events;
using Reki.Domain.Player;

namespace Reki.Application.Events;

public sealed record AnswerEventCommand(TelegramUserId TelegramUserId, EventId EventId, string Text) : IRequest;

