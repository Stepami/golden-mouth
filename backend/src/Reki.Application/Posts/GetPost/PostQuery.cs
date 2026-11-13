using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GetPost;

public sealed record PostQuery(
    PostId PostId,
    WorldId WorldId)
    : IQuery<GamePost?>;