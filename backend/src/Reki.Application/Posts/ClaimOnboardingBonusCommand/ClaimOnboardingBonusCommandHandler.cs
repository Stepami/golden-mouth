using Mediator;
using Reki.Application.Player;
using Reki.Domain.Bonuses;

namespace Reki.Application.Posts.ClaimOnboardingBonusCommand;

internal sealed class ClaimOnboardingBonusCommandHandler(
    IPostRepository posts,
    IPlayerRepository players,
    IBonusRepository bonuses,
    IUnitOfWork unitOfWork)
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

        if (!post.Comments.Any(comment => comment.AuthorId == authorId))
        {
            return false;
        }

        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var bonus = await bonuses.TryClaimBonus<CrystalsBonus>(playerId, token);
                if (bonus is not null)
                {
                    command.Player.ReplenishCrystals(bonus.Amount);
                    await players.ReplacePlayerAsync(command.Player, token);
                }

                return bonus?.IsClaimed == true;
            },
            cancellationToken);
    }
}
