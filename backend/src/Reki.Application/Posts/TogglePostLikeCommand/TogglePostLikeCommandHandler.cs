using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.TogglePostLikeCommand;

internal sealed class TogglePostLikeCommandHandler(IPostRepository posts, IRekiContextAccessor contextAccessor)
    : ICommandHandler<TogglePostLike, PostDetailResponse>
{
    public async ValueTask<PostDetailResponse> Handle(TogglePostLike command, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Context;
        var world = (await context.Worlds).GetValueOrDefault(command.WorldId)
                    ?? throw new ResourceNotFoundException("World not found.");
        var post = await posts.ToggleLikeAsync(command.PostId, world.Id, command.AuthorId, cancellationToken)
                   ?? throw new ResourceNotFoundException("Post not found.");
        return post.MapPost(world, context.PlayerId);
    }
}
