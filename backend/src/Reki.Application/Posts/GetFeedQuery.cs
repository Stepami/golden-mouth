using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery(long TelegramUserId) : IRequest<IReadOnlyList<GamePost>>;

