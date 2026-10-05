using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record AddReplyCommand(TelegramUserId TelegramUserId, PostId PostId, string Text) : IRequest;

