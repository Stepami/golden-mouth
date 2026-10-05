using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetFeedQuery(long TelegramUserId) : IRequest<IReadOnlyList<GamePost>>;

