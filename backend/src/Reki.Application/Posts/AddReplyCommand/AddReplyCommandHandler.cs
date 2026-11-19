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
    IWorldPlayerCache worldCache,
    IOptionsMonitor<GameBalance> balance,
    IRekiContextAccessor rekiContextAccessor,
    PlayerSafetyGuard safety,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromCommentRequest> feedScheduler,
    IStarVectorStore starVectorStore,
    IProductGoalRepository productGoals) : ICommandHandler<AddReply, ProductGoalResult<ReplyResponse>>
{
    public async ValueTask<ProductGoalResult<ReplyResponse>> Handle(AddReply command, CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(command.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await posts.GetPostAsync(command.PostId, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        var isPlayer = command.AuthorId == player.Id.ToAuthorId();
        if (post.FindRequest(command.RequestId) is { } existing)
        {
            var saved = new ReplyResponse(existing.Id, existing.AuthorId, post.MapPost(world, player.Id), player.Wallet.Energy, world.Player.Subscribers);
            return new ProductGoalResult<ReplyResponse>(saved, []);
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
            var additionalReplierId = await starVectorStore.GetRelevantAdditionalReplierAsync(world, targetStar.Id, comment);
            comment.SetAdditionalReplier(additionalReplierId);
            var bonus = post.Comments.Count(item => item.AuthorId == comment.AuthorId) < thread.BonusReplies ? thread.SubscriberBonus : 0;
            player = player.SpendEnergy(thread.EnergyCost, timeProvider.GetUtcNow());
            world = world.AwardThreadBonus(bonus);
        }

        IReadOnlyList<string> goals = [];
        post.AddComments([comment]);
        if (isPlayer)
        {
            await productGoals.EnsurePlayerAsync(player.Id, cancellationToken);
            goals = await unitOfWork.ExecuteAsync(
                async token =>
                {
                    await posts.SaveReplyAsync(post, token);
                    await players.ReplacePlayerAsync(player, token);
                    await worlds.ReplaceWorldAsync(world, token);
                    return await productGoals.RecordActivityAsync(player.Id, timeProvider.GetUtcNow(), ordinaryPost: false, token);
                },
                _ => worldCache.UpdateWorld(world),
                cancellationToken);

            telemetry.TrackPlayer(
                player,
                "comment_created",
                new Dictionary<string, string>
                {
                    ["id"] = comment.Id.Value.ToString("N"),
                    ["kind"] = comment.ParentCommentId is null ? "comment" : "reply",
                    ["template_id"] = world.TemplateId?.ToString() ?? string.Empty,
                    ["difficulty"] = world.Difficulty.ToString().ToLowerInvariant(),
                },
                world.Id);
            telemetry.TrackWorldAudience(world);
            await feedScheduler.ScheduleFeedAsync(new ScheduleFromCommentRequest(player.Id, world.Id, post.Id, comment.Id));
        }
        else
        {
            await posts.SaveReplyAsync(post, cancellationToken);
        }

        var response = new ReplyResponse(comment.Id, comment.AuthorId, post.MapPost(world, player.Id), player.Wallet.Energy, world.Player.Subscribers);
        return new ProductGoalResult<ReplyResponse>(response, goals);
    }
}
