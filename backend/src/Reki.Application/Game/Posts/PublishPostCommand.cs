using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record PublishPostCommand(long TelegramUserId, string Text) : IRequest<PublishedPost>;

