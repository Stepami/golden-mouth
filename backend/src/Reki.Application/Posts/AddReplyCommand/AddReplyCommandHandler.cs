using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AddReplyCommand;

internal sealed class AddReplyCommandHandler(
    IGameStore store,
    TimeProvider timeProvider,
    IPostRepository posts,
    IOptionsMonitor<GameBalance> balance,
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    PlayerSafetyGuard safety,
    ProductTelemetry telemetry,
    IStarFeedScheduler<ScheduleFromCommentRequest> feedScheduler) : ICommandHandler<AddReply, ReplyResponse>
{
    public async ValueTask<ReplyResponse> Handle(AddReply request, CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        var isPlayer = request.AuthorId == player.Id.ToAuthorId();
        if (!isPlayer && world.FindStar(request.AuthorId) is null)
        {
            throw new RequestConflictException("The reply author is not in this world.");
        }

        if (post.FindRequest(request.RequestId) is { } existing)
        {
            EnsureSameRequest(existing, request);
            return Completed(existing, post, world, player);
        }

        ValidateTarget(post, world, request.TargetCommentId, isPlayer);
        if (isPlayer)
        {
            await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        }
        else
        {
            PlayerSafetyGuard.EnsureOutputAllowed([request.Text]);
        }

        if (isPlayer && player.Wallet.Energy < balance.CurrentValue.Thread.EnergyCost)
        {
            throw new RequestConflictException("Insufficient energy.");
        }

        var comment = new GameComment(
            CommentId.New(),
            request.TargetCommentId,
            request.RequestId,
            request.AuthorId,
            request.Text,
            false,
            []);
        var updatedPost = post.AddComments([comment]);
        if (!isPlayer)
        {
            await posts.SaveReplyAsync(updatedPost, cancellationToken);
            return Completed(comment, updatedPost, world, player);
        }

        var currentBalance = balance.CurrentValue;
        var bonus = post.Comments.Count(item => item.AuthorId == request.AuthorId) < currentBalance.Thread.BonusReplies
            ? currentBalance.Thread.SubscriberBonus : 0;
        var updatedPlayer = player.SpendEnergy(currentBalance.Thread.EnergyCost, timeProvider.GetUtcNow());
        var updatedWorld = world.AwardThreadBonus(bonus);
        if (!await store.TryCommitReplyAsync(
                updatedPost,
                post.Version,
                request.RequestId,
                updatedPlayer,
                player.Version,
                updatedWorld,
                world.Version,
                cancellationToken))
        {
            return await ReplayOrConflictAsync(request, cancellationToken);
        }

        telemetry.TrackWorldAudience(updatedWorld);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromCommentRequest(player.Id, world.Id, post.Id, comment.Id));
        return Completed(comment, updatedPost, updatedWorld, updatedPlayer);
    }

    private static ReplyResponse Completed(GameComment comment, GamePost post, World world, PlayerProfile player) =>
        new(comment.Id, comment.AuthorId, post.MapPost(world, player.Id), player.Wallet.Energy, world.Player.Subscribers);

    private static void EnsureSameRequest(GameComment comment, AddReply request)
    {
        if (comment.AuthorId != request.AuthorId || comment.ParentCommentId != request.TargetCommentId ||
            !string.Equals(comment.Text, request.Text, StringComparison.Ordinal))
        {
            throw new RequestConflictException("The request identifier belongs to another reply.");
        }
    }

    private static void ValidateTarget(GamePost post, World world, CommentId? targetId, bool isPlayer)
    {
        if (targetId is null)
        {
            if (isPlayer && world.FindStar(post.AuthorId) is null)
            {
                throw new RequestConflictException("The post author is not a star of this world.");
            }

            return;
        }

        var target = post.Comments.FirstOrDefault(item => item.Id == targetId && !item.Hidden)
            ?? throw new ResourceNotFoundException("Comment not found.");
        if (isPlayer && world.FindStar(target.AuthorId) is null)
        {
            throw new RequestConflictException("The target is not a star of this world.");
        }

        if (!isPlayer && target.AuthorId != world.Player.PlayerId.ToAuthorId())
        {
            throw new RequestConflictException("A star can reply only to the player.");
        }
    }

    private async ValueTask<ReplyResponse> ReplayOrConflictAsync(AddReply request, CancellationToken cancellationToken)
    {
        var context = rekiContextAccessor.Context;
        context.Reload();
        var player = await context.Player;
        var world = (await context.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        var existing = post.FindRequest(request.RequestId)
            ?? throw new RequestConflictException("The reply could not be saved.");
        EnsureSameRequest(existing, request);
        return Completed(existing, post, world, player);
    }
}
