using Mediator;

namespace Reki.Application.Social;

public sealed record FriendsQuery : IQuery<IReadOnlyList<FriendSummary>>;

