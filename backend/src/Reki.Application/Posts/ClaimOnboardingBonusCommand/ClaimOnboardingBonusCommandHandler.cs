using Mediator;

namespace Reki.Application.Posts.ClaimOnboardingBonusCommand;

internal sealed class ClaimOnboardingBonusCommandHandler(IPostRepository posts)
    : ICommandHandler<ClaimOnboardingBonus, bool>
{
    public async ValueTask<bool> Handle(ClaimOnboardingBonus command, CancellationToken cancellationToken)
    {
        var playerId = command.Player.Id;
        if (await posts.CountPostsAsync(playerId, cancellationToken) != 1)
        {
            return false;
        }

        var post = await posts.GetPostAsync(command.PostId, cancellationToken);
        var authorId = playerId.ToAuthorId();
        if (post is null || post.OwnerId != playerId || post.AuthorId != authorId)
        {
            return false;
        }

        return post.Comments.Any(comment => comment.AuthorId == authorId);
    }
}
