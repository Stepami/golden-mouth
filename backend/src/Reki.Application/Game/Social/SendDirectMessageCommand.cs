using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record SendDirectMessageCommand(long TelegramUserId, string StarId, string Text)
    : IRequest<SentDirectMessage>;

