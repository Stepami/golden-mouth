using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPublication;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Posts.Responses.PublicationStatusLiveEvent;
using Reki.Application.RekiContext;
using Reki.Application.Telemetry;
using Reki.Domain.Player;

namespace Reki.Application.Posts.GeneratePublicationCommand;

internal sealed partial class GeneratePublicationCommandHandler(
    IPublicationRepository publicationRepository,
    IOptions<GameBalance> balance,
    TimeProvider timeProvider,
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    IStarTurnGenerator generator,
    ILogger<GeneratePublicationCommandHandler> logger,
    ProductTelemetry analytics,
    PlayerSafetyGuard safety) : IStreamCommandHandler<GeneratePublication, PublicationStatusLiveEvent>
{
    private static readonly TimeSpan FailedRetention = TimeSpan.FromDays(1);

    public async IAsyncEnumerable<PublicationStatusLiveEvent> Handle(
        GeneratePublication request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out _))
        {
            throw new RequestConflictException("The publication identifier is invalid.");
        }

        var rekiContext = rekiContextAccessor.Context;
        var storedPlayer = await rekiContext.Player;
        var world = (await rekiContext.Worlds).GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        var player = storedPlayer;

        var requestHash = ComputeRequestHash(request with { WorldId = world.Id });
        var existing = await sender.Send(new PublicationQuery(request.Id), cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id, existing.Failure);
            yield break;
        }

        if (player.Energy < balance.Value.PostEnergyCost)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var publication = new Publication(
            request.Id,
            player.Id,
            request.Text,
            requestHash,
            request.OwnWords,
            request.WantId,
            PublicationStatusType.Pending,
            [],
            null,
            timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime,
            world.Id);
        if (!await publicationRepository.TryCreatePublicationAsync(publication, cancellationToken))
        {
            existing = await sender.Send(new PublicationQuery(request.Id), cancellationToken)
                ?? throw new RequestConflictException("The publication changed concurrently.");
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id, existing.Failure);
            yield break;
        }

        yield return new PublicationStatusLiveEvent(PublicationStatusType.Pending, request.Id);
        var failure = new PublicationFailure("technical_failure", "uncertain", PublicationSafetyPolicy.Version);
        var failed = false;
        var ready = false;
        try
        {
            StarPostContext? context = null;
            try
            {
                context = await CreatePostContextAsync(request, player, world, cancellationToken);
                await safety.EnsureInputAllowedAsync(player, [request.Text], cancellationToken);
            }
            catch (Exception exception)
            {
                failure = FailureOf(exception);
                GenerationFailed(exception);
                failed = true;
            }

            if (context is null)
            {
                failed = true;
            }

            if (!failed && context is not null)
            {
                await using var updates = generator
                    .StreamPostTurnAsync(context, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                while (!failed)
                {
                    var hasUpdate = false;
                    try
                    {
                        hasUpdate = await updates.MoveNextAsync();
                        if (hasUpdate)
                        {
                            var update = updates.Current;
                            await publicationRepository.AppendPublicationPartAsync(
                                request.Id,
                                new PublicationPart(update.Text, null, null),
                                cancellationToken);
                            await PersistActionsAsync(request.Id, update, context, cancellationToken);
                        }
                    }
                    catch (Exception exception)
                    {
                        failure = FailureOf(exception);
                        GenerationFailed(exception);
                        failed = true;
                    }

                    if (failed || !hasUpdate)
                    {
                        break;
                    }

                    yield return new PublicationStatusLiveEvent(PublicationStatusType.Pending, request.Id);
                }
            }

            if (!failed)
            {
                try
                {
                    var complete = await sender.Send(new PublicationQuery(request.Id), cancellationToken)
                                   ?? throw new InvalidOperationException("The publication disappeared during generation.");
                    var turn = complete.AssembleTurn();
                    PlayerSafetyGuard.EnsureOutputAllowed([turn.Comment, turn.Hook]);
                    await publicationRepository.MarkPublicationReadyAsync(request.Id, cancellationToken);
                    ready = true;
                }
                catch (Exception exception)
                {
                    failure = FailureOf(exception);
                    GenerationFailed(exception);
                    failed = true;
                }
            }
        }
        finally
        {
            if (!ready)
            {
                var expiresAtUtc = timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime;
                await publicationRepository.MarkPublicationFailedAsync(request.Id, expiresAtUtc, failure, CancellationToken.None);
                analytics.TrackPlayer(player, "post_failed", new Dictionary<string, string> { ["id"] = request.Id });
            }
        }

        yield return new PublicationStatusLiveEvent(
            failed ? PublicationStatusType.Failed : PublicationStatusType.ReadyForModeration,
            request.Id,
            failed ? failure : null);
    }

    private static PublicationFailure FailureOf(Exception exception) => exception switch
    {
        ContentRejectedException rejected => new("content_rejected", rejected.Category, PublicationSafetyPolicy.Version),
        UnsafeGeneratedContentException rejected => new("unsafe_generated_content", rejected.Category, PublicationSafetyPolicy.Version),
        CrisisDetectedException => new("crisis", "crisis", PublicationSafetyPolicy.Version),
        _ => new("technical_failure", "uncertain", PublicationSafetyPolicy.Version),
    };

    private static void EnsureSameRequest(
        Publication publication,
        PlayerProfile player,
        string requestHash)
    {
        if (publication.PlayerId != player.Id || publication.RequestHash != requestHash)
        {
            throw new RequestConflictException("The idempotency key belongs to another publication.");
        }
    }

    private static string ComputeRequestHash(GeneratePublication request)
    {
        var payload = $"{request.WorldId.Value}:{request.Text.Length}:{request.Text}:{request.OwnWords}:{request.WantId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string GetStage(long subscribers) => subscribers switch
    {
        < 1000 => "mask",
        < 5000 => "views",
        < 10000 => "strike",
        _ => "new_line",
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publication generation failed")]
    private partial void GenerationFailed(Exception exception);

    private async ValueTask<StarPostContext> CreatePostContextAsync(
        GeneratePublication request,
        PlayerProfile player,
        Domain.Worlds.World world,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var recentPosts = await sender.Send(new WorldPostsQuery(world.Id, PageNumber: 0, PageSize: 3), cancellationToken);
        var hateEnabled = StarTurnPolicy.IsHateEnabled(player, world, balance.Value, now);
        return new StarPostContext(
            request.Text,
            new StarAgentProfile(world, null, player.Address),
            GetStage(world.Player.Subscribers),
            recentPosts.Items.Select(post => post.Text).ToArray(),
            hateEnabled);
    }

    private async ValueTask PersistActionsAsync(
        string publicationId,
        ChatResponseUpdate update,
        StarPostContext context,
        CancellationToken cancellationToken)
    {
        foreach (var call in update.Contents.OfType<FunctionCallContent>())
        {
            var action = StarToolCallParser.Parse([call]).Single();
            var part = action switch
            {
                StarToolCallParser.StarCommentAction comment => new PublicationPart(
                    null,
                    new GeneratedPostTurn(
                        comment.Author,
                        comment.StarId,
                        comment.Text,
                        comment.Hook,
                        string.Empty,
                        comment.Brave,
                        comment.Humor,
                        comment.Topic,
                        comment.Reasons,
                        comment.OnTopic,
                        comment.Tone),
                    null),
                StarToolCallParser.StarReactionAction reaction => new PublicationPart(null, null, reaction.Emoji),
                _ => throw new AiServiceUnavailableException("The model returned an unsupported action."),
            };
            if (part.Comment is { } generated)
            {
                StarTurnPolicy.EnsurePostAllowed(generated, context.Agent.World, context.HateEnabled);
            }

            await publicationRepository.AppendPublicationPartAsync(publicationId, part, cancellationToken);
        }
    }
}
