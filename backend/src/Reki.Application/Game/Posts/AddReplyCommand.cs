using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record AddReplyCommand(long TelegramUserId, string PostId, string Text) : IRequest;

