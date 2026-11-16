using Mediator;

namespace Reki.Application.Posts.LikePostCommand;

internal sealed class LikePostCommandHandler(IPostRepository posts) : ICommandHandler<LikePost>
{
    public async ValueTask<Unit> Handle(LikePost command, CancellationToken cancellationToken)
    {
        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        if (post is null)
        {
            return Unit.Value;
        }

        var like = post.ToggleLike(command.AuthorId);

        await posts.LikePostAsync(command.PostId, command.AuthorId, like, cancellationToken);
        return Unit.Value;
    }
}
