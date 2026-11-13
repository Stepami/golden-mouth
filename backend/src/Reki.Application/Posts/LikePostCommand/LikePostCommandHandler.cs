using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPost;
using Reki.Application.RekiContext;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostCommandHandler(IPostRepository posts, ISender sender, IRekiContextAccessor contextAccessor)
    : ICommandHandler<LikePost>
{
    public async ValueTask<Unit> Handle(LikePost command, CancellationToken cancellationToken)
    {
        var post = await sender.Send(new PostQuery(command.PostId), cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        if (!(await contextAccessor.Context.Worlds).ContainsKey(post.WorldId))
        {
            throw new ResourceNotFoundException("Post not found.");
        }

        await posts.LikePostAsync(command.PostId, command.AuthorId, cancellationToken);
        return Unit.Value;
    }
}
