using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Application.Telemetry;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using static Reki.Application.Common.AI.StarToolCallParser;

namespace Reki.Application.Posts;

public sealed partial class GeneratePublicationHandler(
    IGameStore store,
    IOptions<GameBalance> balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    ISender sender,
    StarTurnGenerator generator,
    ILogger<GeneratePublicationHandler> logger,
    ProductTelemetry analytics) : IStreamRequestHandler<GeneratePublicationCommand, PublicationStatusLiveEvent>
{
    private static readonly TimeSpan FailedRetention = TimeSpan.FromDays(1);

    public async IAsyncEnumerable<PublicationStatusLiveEvent> Handle(
        GeneratePublicationCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out _))
        {
            throw new RequestConflictException("The publication identifier is invalid.");
        }

        var storedPlayer = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var world = await sender.Send(new Worlds.GetWorldQuery(storedPlayer.Id, request.WorldId), cancellationToken);
        var player = gameDay.Normalize(storedPlayer);
        if (!ReferenceEquals(player, storedPlayer) && !await store.TryReplacePlayerAsync(
            player,
            storedPlayer.Version,
            cancellationToken))
        {
            throw new RequestConflictException("The player state changed while starting a new day.");
        }

        var requestHash = ComputeRequestHash(request with { WorldId = world.Id });
        var existing = await sender.Send(new GetPublicationQuery(request.Id), cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id);
            yield break;
        }

        var invite = await store.GetInviteAsync(player.Id, cancellationToken);
        var invited = await store.GetInvitesByInviterAsync(player.Id, cancellationToken);
        var cap = gameDay.GetPostsCap(player, invite, invited);
        if (player.PostsUsed >= cap)
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
            PublicationStatus.Pending,
            [],
            null,
            timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime,
            world.Id);
        if (!await store.TryCreatePublicationAsync(publication, cancellationToken))
        {
            existing = await sender.Send(new GetPublicationQuery(request.Id), cancellationToken)
                ?? throw new RequestConflictException("The publication changed concurrently.");
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id);
            yield break;
        }

        yield return new PublicationStatusLiveEvent(PublicationStatus.Pending, request.Id);
        var failed = false;
        var ready = false;
        try
        {
            StarPostContext? context = null;
            try
            {
                context = await CreatePostContextAsync(request, player, world, cancellationToken);
            }
            catch (Exception exception)
            {
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
                    .StreamPostTurnAsync(context, player, cancellationToken)
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
                            await store.AppendPublicationPartAsync(
                                request.Id,
                                new PublicationPart(update.Text, null, null),
                                cancellationToken);
                            await PersistActionsAsync(request.Id, update, context, cancellationToken);
                        }
                    }
                    catch (Exception exception)
                    {
                        GenerationFailed(exception);
                        failed = true;
                    }

                    if (failed || !hasUpdate)
                    {
                        break;
                    }

                    yield return new PublicationStatusLiveEvent(PublicationStatus.Pending, request.Id);
                }
            }

            if (!failed)
            {
                try
                {
                    var complete = await sender.Send(new GetPublicationQuery(request.Id), cancellationToken)
                                   ?? throw new InvalidOperationException("The publication disappeared during generation.");
                    _ = complete.AssembleTurn();
                    await store.MarkPublicationReadyAsync(request.Id, cancellationToken);
                    ready = true;
                }
                catch (Exception exception)
                {
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
                await store.MarkPublicationFailedAsync(request.Id, expiresAtUtc, CancellationToken.None);
                analytics.TrackPlayer(player, "post_failed", new Dictionary<string, string> { ["id"] = request.Id }, invite is not null);
            }
        }

        yield return new PublicationStatusLiveEvent(
            failed ? PublicationStatus.Failed : PublicationStatus.ReadyForModeration,
            request.Id);
    }

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

    private static string ComputeRequestHash(GeneratePublicationCommand request)
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
        GeneratePublicationCommand request,
        PlayerProfile player,
        Reki.Domain.Worlds.World world,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var recentPosts = await sender.Send(new GetChannelPostsQuery(world.Id, player.Id, 3), cancellationToken);
        var hateEnabled = StarTurnPolicy.IsHateEnabled(player, world, balance.Value, now);
        return new StarPostContext(
            request.Text,
            new StarAgentProfile(world, null, [], player.Address),
            GetStage(world.Player.Subscribers),
            recentPosts.Select(post => post.Text).ToArray(),
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
            var action = Parse([call]).Single();
            var part = action switch
            {
                StarCommentAction comment => new PublicationPart(
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
                StarReactionAction reaction => new PublicationPart(null, null, reaction.Emoji),
                _ => throw new AiServiceUnavailableException("The model returned an unsupported action."),
            };
            if (part.Comment is { } generated)
            {
                StarTurnPolicy.EnsurePostAllowed(generated, context.Agent.World, context.HateEnabled);
            }

            await store.AppendPublicationPartAsync(publicationId, part, cancellationToken);
        }
    }
}