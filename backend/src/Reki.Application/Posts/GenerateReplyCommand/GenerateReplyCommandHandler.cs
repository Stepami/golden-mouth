using System.Runtime.CompilerServices;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Post;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GetPost;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

internal sealed partial class GenerateReplyCommandHandler(
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    StarTurnPolicy turnPolicy,
    PlayerSafetyGuard safety,
    IStarTurnGenerator generator,
    ILogger<GenerateReplyCommandHandler> logger,
    IGameStore store,
    IPostRepository posts,
    IInvitationRepository invitationRepository,
    IStarFeedScheduler<ScheduleFromPostRequest> feedScheduler,
    IOptionsMonitor<GameBalance> balance,
    PostProgressionCalculator calculator,
    TimeProvider timeProvider,
    ProductTelemetry telemetry) : IStreamCommandHandler<GenerateReply, ReplyGenerationLiveEvent>
{
    private const double AdditionalStarProbability = 0.5;

    public async IAsyncEnumerable<ReplyGenerationLiveEvent> Handle(
        GenerateReply request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rekiContext = rekiContextAccessor.Context;
        var player = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var post = await sender.Send(new PostQuery(request.PostId, world.Id), cancellationToken);
        if (request.SourceCommentId is null)
        {
            if (request.AuthorId is not null || request.Additional || post.AuthorId != player.Id.ToAuthorId())
            {
                throw new RequestConflictException("Invalid post generation request.");
            }

            await foreach (var item in GeneratePostAsync(post, player, world, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        if (request.AuthorId is null)
        {
            throw new RequestConflictException("The thread author is required.");
        }

        var star = world.FindStar(request.AuthorId.Value)
            ?? throw new RequestConflictException("The author is not a star of this world.");
        var source = post.Comments.FirstOrDefault(item =>
                item.Id == request.SourceCommentId && !item.Hidden && item.AuthorId == player.Id.ToAuthorId())
            ?? throw new ResourceNotFoundException("Player comment not found.");
        var primary = source.ParentCommentId is { } parentId
            ? world.FindStar(post.Comments.First(item => item.Id == parentId).AuthorId)
            : world.FindStar(post.AuthorId);
        if (primary is null)
        {
            throw new RequestConflictException("The thread star is no longer in this world.");
        }

        var others = world.Stars.Where(item => item.Id != primary.Id).ToArray();
        var bytes = source.Id.Value.ToByteArray();
        StarId? additionalStarId = others.Length > 0 &&
                                   BitConverter.ToUInt32(bytes, 0) / 4294967296d < AdditionalStarProbability
            ? others[(int)(BitConverter.ToUInt32(bytes, 4) % (uint)others.Length)].Id
            : null;
        if (request.Additional ? additionalStarId != star.Id : primary.Id != star.Id)
        {
            throw new RequestConflictException("The star does not belong to the selected thread.");
        }

        var branch = source.ParentCommentId is { } parent ? post.GetBranch(parent) : [];
        var turnContext = new StarReplyContext(
            new StarAgentProfile(world, star.Id, player.Address),
            star.Id,
            post.Text,
            branch,
            source.Text,
            turnPolicy.IsHateEnabled(player, world),
            request.Additional);
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, request.AuthorId, null, null, null);
        var text = new StringBuilder();
        var hasToolCall = false;
        await using var updates = generator.StreamReplyTurnAsync(turnContext, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            bool hasUpdate;
            var failed = false;
            try
            {
                hasUpdate = await updates.MoveNextAsync();
            }
            catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) &&
                                              (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                GenerationFailed();
                failed = true;
                hasUpdate = false;
            }

            if (failed)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, request.AuthorId, null, null, "ai_unavailable");
                yield break;
            }

            if (!hasUpdate)
            {
                break;
            }

            text.Append(string.Concat(updates.Current.Contents.OfType<TextContent>().Select(chunk => chunk.Text)));
            hasToolCall |= updates.Current.Contents.Any(content => content is FunctionCallContent);

            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, request.AuthorId, null, null, null);
        }

        ReplyGenerationLiveEvent outcome;
        try
        {
            var reply = text.ToString().Trim();
            if (hasToolCall || reply.Length is 0 or > 500)
            {
                throw new AiServiceUnavailableException("The model returned an invalid thread reply.");
            }

            PlayerSafetyGuard.EnsureOutputAllowed([reply]);
            outcome = new ReplyGenerationLiveEvent(
                ReplyGenerationStatus.Completed,
                request.AuthorId,
                reply,
                request.Additional ? null : additionalStarId,
                null);
        }
        catch (UnsafeGeneratedContentException)
        {
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, request.AuthorId, null, null, "unsafe_generated_content");
        }
        catch (AiServiceUnavailableException)
        {
            GenerationFailed();
            outcome = new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, request.AuthorId, null, null, "ai_unavailable");
        }

        yield return outcome;
    }

    private static double GetMedian(long[] values) =>
        values.Length % 2 == 0
            ? Math.Max(1, (values[(values.Length / 2) - 1] + values[values.Length / 2]) / 2.0)
            : Math.Max(1, values[values.Length / 2]);

    private async IAsyncEnumerable<ReplyGenerationLiveEvent> GeneratePostAsync(
        GamePost post,
        PlayerProfile player,
        World world,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Generating, null, null, null, null);
        if (post.EvaluationApplied)
        {
            if (post.PredictedAuthorId is not { } author || world.FindStar(author) is not { } star)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, "ai_unavailable");
                yield break;
            }

            var existing = post.Comments.FirstOrDefault(comment => comment.AuthorId == author && comment.ParentCommentId is null);
            if (existing is not null)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Completed, author, existing.Text, null, null);
                yield break;
            }

            var context = new StarReplyContext(new StarAgentProfile(world, star.Id, player.Address), star.Id, post.Text, [], post.Text, turnPolicy.IsHateEnabled(player, world), false);
            var text = new StringBuilder();
            var invalid = false;
            await using var stream = generator.StreamReplyTurnAsync(context, cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                bool next;
                try
                {
                    next = await stream.MoveNextAsync();
                }
                catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) && (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
                {
                    invalid = true;
                    break;
                }

                if (!next)
                {
                    break;
                }

                text.Append(string.Concat(stream.Current.Contents.OfType<TextContent>().Select(chunk => chunk.Text)));
                invalid |= stream.Current.Contents.Any(content => content is FunctionCallContent);
            }

            var reply = text.ToString().Trim();
            if (invalid || reply.Length is 0 or > 500)
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, author, null, null, "ai_unavailable");
                yield break;
            }

            if (!PlayerSafetyGuard.IsOutputAllowed(reply))
            {
                yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, author, null, null, "unsafe_generated_content");
                yield break;
            }

            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Completed, author, reply, null, null);
            yield break;
        }

        var recent = await sender.Send(new WorldPostsQuery(world.Id, null, 3), cancellationToken);
        var stage = world.Player.Subscribers switch { < 1000 => "mask", < 5000 => "views", < 10000 => "strike", _ => "new_line" };
        var postContext = new StarPostContext(post.Text, new StarAgentProfile(world, null, player.Address), stage, recent.Items.Where(item => item.Id != post.Id).Select(item => item.Text).ToArray(), turnPolicy.IsHateEnabled(player, world));
        var actions = new List<StarToolCallParser.IStarAction>();
        var failed = false;
        await using (var stream = generator.StreamPostTurnAsync(postContext, cancellationToken).GetAsyncEnumerator(cancellationToken))
        {
            while (true)
            {
                bool next;
                try
                {
                    next = await stream.MoveNextAsync();
                    if (next)
                    {
                        actions.AddRange(StarToolCallParser.Parse(stream.Current.Contents.OfType<FunctionCallContent>()));
                    }
                }
                catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException) && (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
                {
                    failed = true;
                    break;
                }

                if (!next)
                {
                    break;
                }
            }
        }

        GeneratedPostTurn? turn = null;
        var code = "ai_unavailable";
        try
        {
            var comment = actions.OfType<StarToolCallParser.StarCommentAction>().Single();
            var reactions = actions.OfType<StarToolCallParser.StarReactionAction>().ToArray();
            if (failed || reactions.Length > 1)
            {
                throw new AiServiceUnavailableException("Invalid actions.");
            }

            var star = world.FindStar(comment.StarId) ?? throw new AiServiceUnavailableException("Invalid star.");
            turn = new GeneratedPostTurn(star.Name, star.Id, comment.Text, comment.Hook, reactions.FirstOrDefault()?.Emoji ?? "🔥", comment.Brave, comment.Humor, comment.Topic, comment.Reasons, comment.OnTopic, comment.Tone);
            turnPolicy.EnsurePostAllowed(turn, player, world);
            PlayerSafetyGuard.EnsureOutputAllowed([turn.Comment, turn.Hook]);
        }
        catch (UnsafeGeneratedContentException)
        {
            code = "unsafe_generated_content";
            turn = null;
        }
        catch (Exception exception) when (exception is not (OutOfMemoryException or AccessViolationException))
        {
            turn = null;
        }

        if (turn is null)
        {
            telemetry.TrackPlayer(player, "post_failed", new Dictionary<string, string> { ["id"] = post.Id.ToString() });
            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, code);
            yield break;
        }

        rekiContextAccessor.Context.Reload();
        player = await rekiContextAccessor.Context.Player;
        world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(world.Id)
            ?? throw new ResourceNotFoundException("World not found.");
        post = await posts.GetPostAsync(post.Id, world.Id, cancellationToken)
            ?? throw new ResourceNotFoundException("Post not found.");
        if (post.EvaluationApplied)
        {
            await foreach (var item in GeneratePostAsync(post, player, world, cancellationToken))
            {
                yield return item;
            }

            yield break;
        }

        turnPolicy.EnsurePostAllowed(turn, player, world);
        await safety.EnsureInputAllowedAsync(player, [post.Text], cancellationToken);

        var wantCompleted = post.WantId is not null && world.Player.Wants.Any(want => want.Id == post.WantId && !want.Done);
        var progression = calculator.Calculate(player with { EnergyUsed = post.EnergyIndex - 1 }, world, turn, post.OwnWords, wantCompleted, post.RequestId ?? post.Id.ToString());
        var current = balance.CurrentValue;
        var updatedWorld = world.PublishPost(progression.SubscriberDelta, progression.Viral, progression.Skills, wantCompleted ? post.WantId : null, turn.StarId, timeProvider.GetUtcNow(), current.Relations.Minimum, current.Relations.Maximum, current.Relations.MaxGainPerDay, current.Relations.StarCardAt);
        var updatedPlayer = player with { Skills = progression.Skills, Version = player.Version + 1, HitOwnWorldMilestone = player.HitOwnWorldMilestone || updatedWorld.Player.Subscribers >= current.OwnWorldSubscribers };
        var views = Math.Max(48, updatedWorld.Player.Subscribers);
        var previousViews = recent.Items.Where(item => item.Id != post.Id).Select(item => item.Views).Order().ToArray();
        var median = previousViews.Length == 0 ? views : GetMedian(previousViews);
        var updatedPost = post with { ReachRatio = views / median, EvaluationApplied = true, PredictedAuthorId = turn.StarId.ToAuthorId(), Emoji = turn.Reaction, Hook = turn.Hook, Reasons = turn.Reasons.Select(reason => new PostReason(reason.Icon, reason.Label)).ToArray(), SubscriberDelta = progression.SubscriberDelta, Quality = progression.Quality, Viral = progression.Viral, Flop = progression.Flop, Views = views, Version = post.Version + 1 };
        var invite = await invitationRepository.GetPendingInviteAsync(player.Id, cancellationToken);
        var updatedInvite = invite?.UseInviteeBonus(timeProvider.GetUtcNow()).UseInviterBonus(timeProvider.GetUtcNow());
        updatedPlayer = updatedPlayer with { Energy = updatedPlayer.Energy + (updatedInvite?.BonusAmount ?? 0) };
        if (!await store.TryApplyPostAsync(updatedPost, post.Version, updatedPlayer, player.Version, updatedWorld, world.Version, updatedInvite, current.InviteBonusMaxPerDay, cancellationToken))
        {
            var saved = await posts.GetPostAsync(post.Id, world.Id, cancellationToken);
            if (saved is { EvaluationApplied: true })
            {
                rekiContextAccessor.Context.Reload();
                player = await rekiContextAccessor.Context.Player;
                world = (await rekiContextAccessor.Context.Worlds).GetValueOrDefault(world.Id)
                    ?? throw new ResourceNotFoundException("World not found.");
                await foreach (var item in GeneratePostAsync(saved, player, world, cancellationToken))
                {
                    yield return item;
                }

                yield break;
            }

            yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Failed, null, null, null, "ai_unavailable");
            yield break;
        }

        telemetry.TrackWorldAudience(updatedWorld);
        PublishPostCommandHandler.TrackProgression(player, updatedPlayer, world, updatedWorld, updatedPost, turn, telemetry);
        await feedScheduler.ScheduleFeedAsync(new ScheduleFromPostRequest(player.Id, world.Id, post.Id));
        yield return new ReplyGenerationLiveEvent(ReplyGenerationStatus.Completed, turn.StarId.ToAuthorId(), turn.Comment, null, null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Thread generation failed")]
    private partial void GenerationFailed();
}
