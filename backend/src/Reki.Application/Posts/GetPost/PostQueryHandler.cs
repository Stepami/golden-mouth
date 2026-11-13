using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.GetPost;

internal sealed class PostQueryHandler(IPostRepository postRepository)
    : IQueryHandler<PostQuery, GamePost?>
{
    public async ValueTask<GamePost?> Handle(
        PostQuery query,
        CancellationToken cancellationToken)
        => await postRepository.GetPostAsync(query.PostId, cancellationToken);
}
