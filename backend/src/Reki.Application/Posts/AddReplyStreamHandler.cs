using System.Runtime.CompilerServices;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Player;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed partial class AddReplyStreamHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    ISender sender,
    GameDay gameDay,
    TimeProvider timeProvider,
    PlayerSafetyGuard safety,
    StarTurnGenerator generator,
    ILogger<AddReplyStreamHandler> logger) : IStreamRequestHandler<AddReplyCommand, ReplyLiveEvent>
{
    public async IAsyncEnumerable<ReplyLiveEvent> Handle(
        AddReplyCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId) || request.RequestId.Length > 64 ||
            string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 1000)
        {
            throw new RequestConflictException("The reply request is invalid.");
        }

        var storedPlayer = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(storedPlayer.Id, request.WorldId), cancellationToken);
        var post = await sender.Send(new GetPostQuery(request.PostId, storedPlayer.Id, world.Id), cancellationToken);
        var target = ValidateTarget(post, world, request.TargetCommentId);
        await safety.EnsureInputAllowedAsync(storedPlayer, [request.Text], cancellationToken);
        var player = gameDay.Normalize(storedPlayer);
        var cap = await GetCapAsync(player, cancellationToken);
        if (post.FindRequest(request.RequestId) is { } existing)
        {
            EnsureSameRequest(existing, request, player.Id);
            yield return await ReplayAsync(request, player.Id, cancellationToken);
            yield break;
        }

        EnsureEnergy(player, cap);
        var context = new StarReplyContext(
            new StarAgentProfile(world, null, [], player.Address),
            StarId.From(target.AuthorId.Value.Item2),
            post.Text,
            post.GetBranch(target.Id),
            request.Text,
            StarTurnPolicy.IsHateEnabled(player, world, balance.Value, timeProvider.GetUtcNow()));
        yield return new ReplyLiveEvent("generating", request.RequestId);
        var calls = new List<FunctionCallContent>();
        await using var updates = generator.StreamReplyTurnAsync(context, player, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasUpdate;
            var failed = false;
            try
            {
                hasUpdate = await updates.MoveNextAsync();
                if (hasUpdate)
                {
                    calls.AddRange(updates.Current.Contents.OfType<FunctionCallContent>());
                }
            }

            // Provider exceptions after SSE headers need a safe terminal event; cancellation and
            // critical runtime failures propagate instead of being presented as a model error.
            catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) &&
                                              (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                GenerationFailed();
                hasUpdate = false;
                failed = true;
            }

            if (failed)
            {
                yield return new ReplyLiveEvent("failed", request.RequestId) { Code = "ai_unavailable" };
                yield break;
            }

            if (!hasUpdate)
            {
                break;
            }

            yield return new ReplyLiveEvent("generating", request.RequestId);
        }

        GeneratedReplyTurn? turn = null;
        try
        {
            turn = ReplyTurnPolicy.Parse(calls, context);
        }
        catch (AiServiceUnavailableException)
        {
            GenerationFailed();
        }

        if (turn is null)
        {
            yield return new ReplyLiveEvent("failed", request.RequestId) { Code = "ai_unavailable" };
            yield break;
        }

        // The preview is safe but remains unpublished until the transaction succeeds.
        yield return new ReplyLiveEvent("generating", request.RequestId) { Text = turn.Text };
        ReplyLiveEvent outcome;
        try
        {
            outcome = await CommitAsync(request, turn, player.Id, cancellationToken);
        }
        catch (RequestConflictException)
        {
            outcome = new ReplyLiveEvent("failed", request.RequestId) { Code = "request_conflict" };
        }

        yield return outcome;
    }

    private static ReplyLiveEvent Completed(AddReplyCommand request, GamePost post, World world, PlayerProfile player, int cap) =>
        new("completed", request.RequestId)
        {
            Post = PostContractMapper.MapPost(post, world, player.Id),
            EnergyLeft = Math.Max(0, cap - player.EnergyUsed),
            Subscribers = world.Player.Subscribers,
        };

    private static void EnsureSameRequest(GameComment comment, AddReplyCommand request, PlayerId playerId)
    {
        if (comment.AuthorId != playerId.ToAuthor() || comment.ParentCommentId != request.TargetCommentId ||
            !string.Equals(comment.Text, request.Text, StringComparison.Ordinal))
        {
            throw new RequestConflictException("The request identifier belongs to another reply.");
        }
    }

    private GameComment ValidateTarget(GamePost post, World world, CommentId targetId)
    {
        var target = post.Comments.FirstOrDefault(comment => comment.Id == targetId && !comment.Hidden)
                     ?? throw new ResourceNotFoundException("Comment not found.");
        if (target.AuthorId.Value.Item1 != AuthorKind.Star ||
            world.FindStar(StarId.From(target.AuthorId.Value.Item2)) is null)
        {
            throw new RequestConflictException("The target is not a star of this world.");
        }

        if (post.GetBranch(targetId).Count + 1 > balance.Value.Thread.BranchDepth)
        {
            throw new RequestConflictException("The maximum branch depth has been reached.");
        }

        return target;
    }

    private async ValueTask<ReplyLiveEvent> CommitAsync(AddReplyCommand request, GeneratedReplyTurn turn, PlayerId playerId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var storedPlayer = await store.GetPlayerAsync(playerId, cancellationToken)
                               ?? throw new ResourceNotFoundException("Player not found.");
            var player = gameDay.Normalize(storedPlayer);
            var world = await sender.Send(new Worlds.GetWorldQuery(playerId, request.WorldId), cancellationToken);
            var post = await sender.Send(new GetPostQuery(request.PostId, playerId, world.Id), cancellationToken);
            var cap = await GetCapAsync(player, cancellationToken);
            if (post.FindRequest(request.RequestId) is { } existing)
            {
                EnsureSameRequest(existing, request, playerId);
                return await ReplayAsync(request, playerId, cancellationToken);
            }

            var target = ValidateTarget(post, world, request.TargetCommentId);
            if (target.AuthorId != turn.StarId.ToAuthor() ||
                (turn.AdditionalStarId is { } other && world.FindStar(other) is null))
            {
                throw new RequestConflictException("The world changed during generation.");
            }

            var hateEnabled = StarTurnPolicy.IsHateEnabled(player, world, balance.Value, timeProvider.GetUtcNow());
            if ((turn.Tone == "hate" || turn.AdditionalTone == "hate") && !hateEnabled)
            {
                throw new RequestConflictException("The world safety settings changed during generation.");
            }

            EnsureEnergy(player, cap);
            var bonus = post.Comments.Count(comment =>
                            comment.AuthorId == playerId.ToAuthor() && comment.ParentCommentId is not null)
                        < balance.Value.Thread.BonusReplies
                ? balance.Value.Thread.SubscriberBonus
                : 0;
            var playerComment = new GameComment(
                CommentId.New(),
                target.Id,
                request.RequestId,
                playerId.ToAuthor(),
                request.Text,
                false,
                turn.Emoji is { } emoji ? [new CommentReaction(turn.StarId.ToAuthor(), emoji)] : []);
            List<GameComment> comments =
            [
                playerComment,
                new GameComment(CommentId.New(), playerComment.Id, null, turn.StarId.ToAuthor(), turn.Text, false, [])
            ];
            if (turn.AdditionalStarId is { } additional && turn.AdditionalText is { } text)
            {
                comments.Add(new GameComment(CommentId.New(), null, null, additional.ToAuthor(), text, false, []));
            }

            var updatedPost = post.AddComments(comments);
            var updatedPlayer = player.SpendEnergy(balance.Value.Thread.EnergyCost, cap);
            var updatedWorld = world.AwardThreadBonus(bonus);
            if (await store.TryCommitReplyAsync(updatedPost, post.Version, request.RequestId, updatedPlayer, storedPlayer.Version, updatedWorld, world.Version, cancellationToken))
            {
                return Completed(request, updatedPost, updatedWorld, updatedPlayer, cap);
            }
        }

        throw new RequestConflictException("The reply changed concurrently.");
    }

    private async ValueTask<ReplyLiveEvent> ReplayAsync(AddReplyCommand request, PlayerId playerId, CancellationToken cancellationToken)
    {
        // Seeing the saved request proves that its energy and bonus transaction has committed.
        // Read the dependent state afterwards rather than returning the preflight snapshot.
        var post = await sender.Send(new GetPostQuery(request.PostId, playerId, request.WorldId), cancellationToken);
        var storedPlayer = await store.GetPlayerAsync(playerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        var player = gameDay.Normalize(storedPlayer);
        var world = await sender.Send(new Worlds.GetWorldQuery(playerId, post.WorldId), cancellationToken);
        var existing = post.FindRequest(request.RequestId) ?? throw new RequestConflictException("The saved reply disappeared.");
        EnsureSameRequest(existing, request, playerId);
        return Completed(request, post, world, player, await GetCapAsync(player, cancellationToken));
    }

    private void EnsureEnergy(PlayerProfile player, int cap)
    {
        if (player.EnergyUsed > cap - balance.Value.Thread.EnergyCost)
        {
            throw new RequestConflictException("Insufficient energy.");
        }
    }

    private async ValueTask<int> GetCapAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        gameDay.GetPostsCap(player, await store.GetInviteAsync(player.Id, cancellationToken), await store.GetInvitesByInviterAsync(player.Id, cancellationToken));

    [LoggerMessage(Level = LogLevel.Warning, Message = "Thread generation failed")]
    private partial void GenerationFailed();
}