using Mediator;
using Reki.Application.Exceptions;
using Reki.Application.Posts.GetPost;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostCommandHandler(IPostRepository posts, ISender sender)
    : ICommandHandler<LikePost>
{
    public async ValueTask<Unit> Handle(LikePost command, CancellationToken cancellationToken)
    {
        _ = await sender.Send(new PostQuery(command.PostId), cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        await posts.LikePostAsync(command.PostId, command.AuthorId, cancellationToken);
        return Unit.Value;
    }
}
