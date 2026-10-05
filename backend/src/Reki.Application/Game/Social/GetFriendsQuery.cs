using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetFriendsQuery(long TelegramUserId) : IRequest<IReadOnlyList<FriendSummary>>;

