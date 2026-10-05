using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record PublishPostCommand(
    TelegramUserId TelegramUserId,
    string Text,
    bool OwnWords,
    string? WantId) : IRequest<PublishedPost>;

