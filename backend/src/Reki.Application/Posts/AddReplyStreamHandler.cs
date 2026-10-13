using System.Runtime.CompilerServices;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Application.Telemetry;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed partial class AddReplyStreamHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    TimeProvider timeProvider,
    PlayerSafetyGuard safety,
    StarTurnGenerator generator,
    ILogger<AddReplyStreamHandler> logger,
    ProductTelemetry telemetry,
    IStarFeedScheduler feedScheduler) : IStreamRequestHandler<AddReplyCommand, ReplyLiveEvent>
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

        var rekiContext = rekiContextAccessor.Context;
        var storedPlayer = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new GetPostQuery(request.PostId, world.Id), cancellationToken);
        var (target, star) = ValidateTarget(post, world, request.TargetCommentId);
        await safety.EnsureInputAllowedAsync(storedPlayer, [request.Text], cancellationToken);
        var player = storedPlayer;
        if (post.FindRequest(request.RequestId) is { } existing)
        {
            EnsureSameRequest(existing, request, player.Id);
            yield return await ReplayAsync(request, player.Id, cancellationToken);
            yield break;
        }

        EnsureEnergy(player);
        var context = new StarReplyContext(
            new StarAgentProfile(world, null, [], player.Address),
            star.Id,
            post.Text,
            post.GetBranch(target.Id),
            request.Text,
            StarTurnPolicy.IsHateEnabled(player, world, balance.Value, timeProvider.GetUtcNow()));
        yield return new ReplyLiveEvent(ReplyStatus.Generating, request.RequestId, null, null, null, null, null);
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
                                              (exception is not OperationCanceledException ||
                                               !cancellationToken.IsCancellationRequested))
            {
                GenerationFailed();
                hasUpdate = false;
                failed = true;
            }

            if (failed)
            {
                yield return new ReplyLiveEvent(ReplyStatus.Failed, request.RequestId, null, null, null, null, "ai_unavailable");
                yield break;
            }

            if (!hasUpdate)
            {
                break;
            }

            yield return new ReplyLiveEvent(ReplyStatus.Generating, request.RequestId, null, null, null, null, null);
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
            yield return new ReplyLiveEvent(ReplyStatus.Failed, request.RequestId, null, null, null, null, "ai_unavailable");
            yield break;
        }

        // The preview is safe but remains unpublished until the transaction succeeds.
        yield return new ReplyLiveEvent(ReplyStatus.Generating, request.RequestId, turn.Text, null, null, null, null);
        ReplyLiveEvent outcome;
        try
        {
            outcome = await CommitAsync(request, turn, player.Id, cancellationToken);
        }
        catch (RequestConflictException)
        {
            outcome = new ReplyLiveEvent(ReplyStatus.Failed, request.RequestId, null, null, null, null, "request_conflict");
        }

        yield return outcome;
    }

    private static ReplyLiveEvent Completed(
        AddReplyCommand request,
        GamePost post,
        World world,
        PlayerProfile player) =>
        new(
            ReplyStatus.Completed,
            request.RequestId,
            Text: null,
            Post: PostContractMapper.MapPost(post, world, player.Id),
            EnergyLeft: player.Energy,
            Subscribers: world.Player.Subscribers,
            Code: null);

    private static void EnsureSameRequest(GameComment comment, AddReplyCommand request, PlayerId playerId)
    {
        if (comment.AuthorId != playerId.ToAuthorId() || comment.ParentCommentId != request.TargetCommentId ||
            !string.Equals(comment.Text, request.Text, StringComparison.Ordinal))
        {
            throw new RequestConflictException("The request identifier belongs to another reply.");
        }
    }

    private static (GameComment Comment, WorldStar Star) ValidateTarget(GamePost post, World world, CommentId targetId)
    {
        var target = post.Comments.FirstOrDefault(comment => comment.Id == targetId && !comment.Hidden)
                     ?? throw new ResourceNotFoundException("Comment not found.");
        var star = world.FindStar(target.AuthorId)
                   ?? throw new RequestConflictException("The target is not a star of this world.");

        return (target, star);
    }

    private async ValueTask<ReplyLiveEvent> CommitAsync(
        AddReplyCommand request,
        GeneratedReplyTurn turn,
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rekiContext = rekiContextAccessor.Context;
        rekiContext.Reload();
        var storedPlayer = await rekiContext.Player;
        var player = storedPlayer;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new GetPostQuery(request.PostId, world.Id), cancellationToken);
        if (post.FindRequest(request.RequestId) is { } existing)
        {
            EnsureSameRequest(existing, request, playerId);
            return await ReplayAsync(request, playerId, cancellationToken);
        }

        await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
        var (target, _) = ValidateTarget(post, world, request.TargetCommentId);
        if (target.AuthorId != turn.StarId.ToAuthorId() ||
            (turn.AdditionalStarId is { } other && world.FindStar(other) is null))
        {
            throw new RequestConflictException("The world changed during generation.");
        }

        var hateEnabled = StarTurnPolicy.IsHateEnabled(player, world, balance.Value, timeProvider.GetUtcNow());
        if ((turn.Tone == "hate" || turn.AdditionalTone == "hate") && !hateEnabled)
        {
            throw new RequestConflictException("The world safety settings changed during generation.");
        }

        EnsureEnergy(player);
        var bonus = post.Comments.Count(comment =>
                        comment.AuthorId == playerId.ToAuthorId() && comment.ParentCommentId is not null)
                    < balance.Value.Thread.BonusReplies
            ? balance.Value.Thread.SubscriberBonus
            : 0;
        var playerComment = new GameComment(
            CommentId.New(),
            target.Id,
            request.RequestId,
            playerId.ToAuthorId(),
            request.Text,
            false,
            turn.Emoji is { } emoji ? [new CommentReaction(turn.StarId.ToAuthorId(), emoji)] : []);
        List<GameComment> comments =
        [
            playerComment,
            new GameComment(CommentId.New(), playerComment.Id, null, turn.StarId.ToAuthorId(), turn.Text, false, [])
        ];
        if (turn.AdditionalStarId is { } additional && turn.AdditionalText is { } text)
        {
            comments.Add(new GameComment(CommentId.New(), null, null, additional.ToAuthorId(), text, false, []));
        }

        var updatedPost = post.AddComments(comments);
        var updatedPlayer = player.SpendEnergy(balance.Value.Thread.EnergyCost);
        var updatedWorld = world.AwardThreadBonus(bonus);
        if (await store.TryCommitReplyAsync(
                updatedPost,
                post.Version,
                request.RequestId,
                updatedPlayer,
                storedPlayer.Version,
                updatedWorld,
                world.Version,
                cancellationToken))
        {
            telemetry.TrackWorldAudience(updatedWorld);
            await feedScheduler.ScheduleAsync(playerId, world.Id, "reply", request.RequestId, CancellationToken.None);
            return Completed(request, updatedPost, updatedWorld, updatedPlayer);
        }

        return new ReplyLiveEvent(ReplyStatus.Failed, request.RequestId, null, null, null, null, "request_conflict");
    }

    /// <summary>
    /// Seeing the saved request proves that its energy and bonus transaction has committed.
    /// Read the dependent state afterwards rather than returning the preflight snapshot.
    /// </summary>
    private async ValueTask<ReplyLiveEvent> ReplayAsync(
        AddReplyCommand request,
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        var post = await sender.Send(new GetPostQuery(request.PostId, request.WorldId), cancellationToken);
        var rekiContext = rekiContextAccessor.Context;
        rekiContext.Reload();
        var storedPlayer = await rekiContext.Player;
        var player = storedPlayer;
        var world = (await rekiContext.Worlds).GetValueOrDefault(post.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var existing = post.FindRequest(request.RequestId) ??
                       throw new RequestConflictException("The saved reply disappeared.");
        EnsureSameRequest(existing, request, playerId);
        return Completed(request, post, world, player);
    }

    private void EnsureEnergy(PlayerProfile player)
    {
        if (player.Energy < balance.Value.Thread.EnergyCost)
        {
            throw new RequestConflictException("Insufficient energy.");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Thread generation failed")]
    private partial void GenerationFailed();
}