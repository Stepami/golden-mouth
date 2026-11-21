using Mediator;
using Reki.Application.Bonuses;
using Reki.Application.Exceptions;
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
        var authorId = playerId.ToAuthorId();

        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                if (await posts.CountPostsAsync(playerId, token) != 1)
                {
                    return false;
                }

                var post = await posts.GetPostAsync(command.PostId, token);
                if (post is null || post.OwnerId != playerId || post.AuthorId != authorId ||
                    !post.Comments.Any(comment => comment.AuthorId == authorId))
                {
                    return false;
                }

                var bonus = await bonuses.GetActiveBonus<CrystalsBonus>(playerId, token);
                if (bonus is not null)
                {
                    var player = await players.GetPlayerAsync(playerId, token)
                                 ?? throw new ResourceNotFoundException("Player not found.");
                    bonus.Claim();
                    player.ReplenishCrystals(bonus.Amount);
                    await bonuses.SaveBonus(bonus, token);
                    await players.ReplacePlayerAsync(player, token);
                }

                return bonus?.IsClaimed == true;
            },
            cancellationToken);
    }
}
