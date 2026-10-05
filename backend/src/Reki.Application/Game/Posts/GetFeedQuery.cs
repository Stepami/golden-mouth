using Mediator;
using Reki.Domain.Game.Posts;

namespace Reki.Application.Game.Posts;

public sealed record GetFeedQuery(long TelegramUserId) : IRequest<IReadOnlyList<GamePost>>;

