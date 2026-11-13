using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.GetPost;

public sealed record PostQuery(PostId PostId)
    : IQuery<GamePost?>;
