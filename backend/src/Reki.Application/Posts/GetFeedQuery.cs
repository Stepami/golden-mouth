using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery(TelegramUserId TelegramUserId) : IRequest<IReadOnlyList<GamePost>>;

