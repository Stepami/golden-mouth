using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery(WorldId WorldId) : IRequest<IReadOnlyList<FeedItemResponse>>;