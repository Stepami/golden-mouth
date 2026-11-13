using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostCommandHandler(IPostRepository posts, IRekiContextAccessor contextAccessor)
    : ICommandHandler<LikePost>
{
    public async ValueTask<Unit> Handle(LikePost command, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Context;
        var world = (await context.Worlds).GetValueOrDefault(command.WorldId)
                    ?? throw new ResourceNotFoundException("World not found.");
        _ = await posts.GetPostAsync(command.PostId, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        await posts.LikePostAsync(command.PostId, command.AuthorId, cancellationToken);
        return Unit.Value;
    }
}
