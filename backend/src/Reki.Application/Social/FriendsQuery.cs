using Mediator;

namespace Reki.Application.Social;

public sealed record FriendsQuery : IRequest<IReadOnlyList<FriendSummary>>;

