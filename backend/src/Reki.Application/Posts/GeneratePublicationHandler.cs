using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Worlds;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using TgMiniAppAuth.AuthContext.User;
using static Reki.Application.Common.AI.StarToolCallParser;

namespace Reki.Application.Posts;

public sealed class GeneratePublicationHandler(
    ITelegramUserAccessor telegramUser,
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    StarTurnGenerator generator) : IStreamRequestHandler<GeneratePublicationCommand, PublicationStatusLiveEvent>
{
    private static readonly TimeSpan FailedRetention = TimeSpan.FromDays(1);

    public async IAsyncEnumerable<PublicationStatusLiveEvent> Handle(
        GeneratePublicationCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        TelegramUserId telegramUserId = TelegramUserId.From(telegramUser.User.Id);
        if (!Guid.TryParse(request.Id, out _))
        {
            throw new RequestConflictException("The publication identifier is invalid.");
        }

        PlayerProfile storedPlayer = await playerReader.GetAsync(telegramUserId, cancellationToken);
        PlayerProfile player = gameDay.Normalize(storedPlayer);
        if (!ReferenceEquals(player, storedPlayer) && !await store.TryReplacePlayerAsync(
            player,
            storedPlayer.Version,
            cancellationToken))
        {
            throw new RequestConflictException("The player state changed while starting a new day.");
        }

        string requestHash = ComputeRequestHash(request);
        Publication? existing = await store.GetPublicationAsync(request.Id, cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id);
            yield break;
        }

        int cap = player.GetPostsCap(balance.FreePostsPerDay, balance.PlusPostsPerDay);
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
            timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime);
        if (!await store.TryCreatePublicationAsync(publication, cancellationToken))
        {
            existing = await store.GetPublicationAsync(request.Id, cancellationToken)
                ?? throw new RequestConflictException("The publication changed concurrently.");
            EnsureSameRequest(existing, player, requestHash);
            yield return new PublicationStatusLiveEvent(existing.Status, request.Id);
            yield break;
        }

        yield return new PublicationStatusLiveEvent(PublicationStatus.Pending, request.Id);
        bool failed = false;
        bool ready = false;
        try
        {
            StarPostContext? context = null;
            try
            {
                context = await CreatePostContextAsync(request, player, cancellationToken);
            }
            catch (Exception exception) when (IsGenerationFailure(exception))
            {
                failed = true;
            }

            if (context is null)
            {
                failed = true;
            }

            if (!failed && context is not null)
            {
                await using IAsyncEnumerator<ChatResponseUpdate> updates = generator
                    .StreamPostTurnAsync(context, player, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                while (!failed)
                {
                    bool hasUpdate = false;
                    try
                    {
                        hasUpdate = await updates.MoveNextAsync();
                        if (hasUpdate)
                        {
                            ChatResponseUpdate update = updates.Current;
                            await store.AppendPublicationPartAsync(
                                request.Id,
                                new PublicationPart(update.Text, null, null),
                                cancellationToken);
                            await PersistActionsAsync(request.Id, update, context, cancellationToken);
                        }
                    }
                    catch (Exception exception) when (IsGenerationFailure(exception))
                    {
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
                    Publication complete = await store.GetPublicationAsync(request.Id, cancellationToken)
                        ?? throw new InvalidOperationException("The publication disappeared during generation.");
                    _ = complete.AssembleTurn();
                    await store.MarkPublicationReadyAsync(request.Id, cancellationToken);
                    ready = true;
                }
                catch (Exception exception) when (IsGenerationFailure(exception))
                {
                    failed = true;
                }
            }
        }
        finally
        {
            if (!ready)
            {
                DateTime expiresAtUtc = timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime;
                await store.MarkPublicationFailedAsync(request.Id, expiresAtUtc, CancellationToken.None);
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
        string payload = $"{request.Text.Length}:{request.Text}:{request.OwnWords}:{request.WantId}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string GetStage(long subscribers) => subscribers switch
    {
        < 1000 => "mask",
        < 5000 => "views",
        < 10000 => "strike",
        _ => "new_line",
    };

    private static bool IsGenerationFailure(Exception exception) => exception is
        AiServiceUnavailableException or
        ContentRejectedException or
        CrisisDetectedException or
        InvalidOperationException or
        OperationCanceledException or
        HttpRequestException or
        TimeoutException;

    private async ValueTask<StarPostContext> CreatePostContextAsync(
        GeneratePublicationCommand request,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        WorldRules? storedRules = await store.GetWorldRulesAsync(player.Id, player.World, cancellationToken);
        WorldRules worldRules = player.World.Value == "own" && storedRules is not null
            ? storedRules
            : WorldRulesDefaults.Create(player.Id, player.World);
        IReadOnlyList<GamePost> recentPosts = await store.GetRecentPostsAsync(player.Id, 3, cancellationToken);
        bool hateEnabled = StarTurnPolicy.IsHateEnabled(player, balance, now);
        return new StarPostContext(
            request.Text,
            new StarAgentProfile(
                player.World,
                worldRules.Title,
                worldRules.Description,
                worldRules.Rules,
                player.Settings.Drama,
                player.Settings.Voice,
                null,
                [],
                player.Address),
            GetStage(player.Subscribers),
            recentPosts.Select(post => post.Text).ToArray(),
            hateEnabled);
    }

    private async ValueTask PersistActionsAsync(
        string publicationId,
        ChatResponseUpdate update,
        StarPostContext context,
        CancellationToken cancellationToken)
    {
        foreach (FunctionCallContent call in update.Contents.OfType<FunctionCallContent>())
        {
            IStarAction action = StarToolCallParser.Parse([call]).Single();
            PublicationPart part = action switch
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
                StarTurnPolicy.EnsurePostAllowed(generated, context.Agent.WorldId, context.HateEnabled);
            }

            await store.AppendPublicationPartAsync(publicationId, part, cancellationToken);
        }
    }
}
