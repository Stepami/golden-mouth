using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery() : IRequest<IReadOnlyList<FeedItemResponse>>;

