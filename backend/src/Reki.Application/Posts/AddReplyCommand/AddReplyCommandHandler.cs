using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.ML;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AddReplyCommand;

internal sealed class AddReplyCommandHandler(
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork,
    IPostRepository posts,
    IPlayerRepository players,
    IWorldRepository worlds,
    IOptionsMonitor<GameBalance> balance,
    IRekiContextAccessor rekiContextAccessor,
    PlayerSafetyGuard safety,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromCommentRequest> feedScheduler,
    IStarVectorStore starVectorStore) : ICommandHandler<AddReply, ReplyResponse>
{
    public async ValueTask<ReplyResponse> Handle(AddReply command, CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(command.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await posts.GetPostAsync(command.PostId, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        if (post.IsRepost)
        {
            throw new RequestConflictException("Open the original post to reply.");
        }

        var quote = post.QuotedPostId is { } quoteId ? await posts.GetPostAsync(quoteId, world.Id, cancellationToken) : null;
        var isPlayer = command.AuthorId == player.Id.ToAuthorId();
        if (post.FindRequest(command.RequestId) is { } existing)
        {
            return new ReplyResponse(existing.Id, existing.AuthorId, post.MapPost(world, player.Id, quote), player.Wallet.Energy, world.Player.Subscribers);
        }

        var comment = new GameComment(
            CommentId.New(),
            command.TargetCommentId,
            command.RequestId,
            command.AuthorId,
            command.Text,
            false,
            [],
            null);
        if (isPlayer)
        {
            await safety.EnsureInputAllowedAsync(player, [comment.Text], cancellationToken);
            var thread = balance.CurrentValue.Thread;
            if (player.Wallet.Energy < thread.EnergyCost)
            {
                throw new RequestConflictException("Insufficient energy.");
            }

            var targetAuthor = comment.ParentCommentId is { } parentId
                ? post.Comments.First(item => item.Id == parentId).AuthorId
                : post.AuthorId;
            var targetStar = world.Stars.First(star => star.Id.ToAuthorId() == targetAuthor);
            comment = comment with
            {
                AdditionalReplierId = await starVectorStore.GetRelevantAdditionalReplierAsync(world, targetStar.Id, comment),
            };
            var bonus = post.Comments.Count(item => item.AuthorId == comment.AuthorId) < thread.BonusReplies ? thread.SubscriberBonus : 0;
            player = player.SpendEnergy(thread.EnergyCost, timeProvider.GetUtcNow());
            world = world.AwardThreadBonus(bonus);
        }

        post = post.AddComments([comment]);
        if (isPlayer)
        {
            await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await posts.SaveReplyAsync(post, token);
                    await players.ReplacePlayerAsync(player, token);
                    await worlds.ReplaceWorldAsync(world, token);
                    return Unit.Value;
                },
                cancellationToken);

            telemetry.TrackWorldAudience(world);
            await feedScheduler.ScheduleFeedAsync(new ScheduleFromCommentRequest(player.Id, world.Id, post.Id, comment.Id));
        }
        else
        {
            await posts.SaveReplyAsync(post, cancellationToken);
        }

        return new ReplyResponse(comment.Id, comment.AuthorId, post.MapPost(world, player.Id, quote), player.Wallet.Energy, world.Player.Subscribers);
    }
}
