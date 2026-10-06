using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Mediator;
using Microsoft.Extensions.AI;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using static Reki.Application.Common.AI.StarToolCallParser;

namespace Reki.Application.Posts;

public sealed class GeneratePublicationHandler(
    IGameStore store,
    GameBalance balance,
    TimeProvider timeProvider,
    GameDay gameDay,
    PlayerReader playerReader,
    StarTurnGenerator generator) : IStreamRequestHandler<GeneratePublicationCommand, PublicationStatus>
{
    private static readonly TimeSpan FailedRetention = TimeSpan.FromDays(1);

    public async IAsyncEnumerable<PublicationStatus> Handle(
        GeneratePublicationCommand request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out _))
        {
            throw new RequestConflictException("The publication identifier is invalid.");
        }

        PlayerProfile storedPlayer = await playerReader.GetAsync(request.TelegramUserId, cancellationToken);
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
            yield return existing.Status;
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
            yield return existing.Status;
            yield break;
        }

        yield return PublicationStatus.Pending;
        bool failed = false;
        try
        {
            await PersistGenerationAsync(request, player, cancellationToken);
        }
        catch (AiServiceUnavailableException)
        {
            failed = true;
        }
        catch (ContentRejectedException)
        {
            failed = true;
        }
        catch (CrisisDetectedException)
        {
            failed = true;
        }
        catch (InvalidOperationException)
        {
            failed = true;
        }
        catch (OperationCanceledException)
        {
            failed = true;
        }
        catch (HttpRequestException)
        {
            failed = true;
        }
        catch (TimeoutException)
        {
            failed = true;
        }

        if (failed)
        {
            DateTime expiresAtUtc = timeProvider.GetUtcNow().Add(FailedRetention).UtcDateTime;
            await store.MarkPublicationFailedAsync(request.Id, expiresAtUtc, CancellationToken.None);
        }

        yield return failed ? PublicationStatus.Failed : PublicationStatus.ReadyForModeration;
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

    private async ValueTask PersistGenerationAsync(
        GeneratePublicationCommand request,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        WorldRules? storedRules = await store.GetWorldRulesAsync(player.Id, player.World, cancellationToken);
        WorldRules worldRules = storedRules ?? WorldRulesDefaults.Create(player.Id, player.World);
        IReadOnlyList<GamePost> recentPosts = await store.GetRecentPostsAsync(player.Id, 3, cancellationToken);
        bool hateEnabled = player.Settings.Drama != "off" &&
            (player.HateDisabledUntilUtc is null || player.HateDisabledUntilUtc <= now);
        var context = new StarPostContext(
            request.Text,
            new StarAgentProfile(
                player.World,
                worldRules.Title,
                worldRules.Description,
                worldRules.Rules,
                player.Settings.Drama,
                player.Settings.Voice,
                null,
                []),
            GetStage(player.Subscribers),
            recentPosts.Select(post => post.Text).ToArray(),
            hateEnabled);
        await foreach (ChatResponseUpdate update in generator.StreamPostTurnAsync(
            context,
            player,
            cancellationToken))
        {
            await store.AppendPublicationPartAsync(
                request.Id,
                new PublicationPart(update.Text, null, null),
                cancellationToken);
            await PersistActionsAsync(request.Id, update, cancellationToken);
        }

        Publication complete = await store.GetPublicationAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("The publication disappeared during generation.");
        _ = complete.AssembleTurn();
        await store.MarkPublicationReadyAsync(request.Id, cancellationToken);
    }

    private async ValueTask PersistActionsAsync(
        string publicationId,
        ChatResponseUpdate update,
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
                        comment.OnTopic),
                    null),
                StarReactionAction reaction => new PublicationPart(null, null, reaction.Emoji),
                _ => throw new AiServiceUnavailableException("The model returned an unsupported action."),
            };
            await store.AppendPublicationPartAsync(publicationId, part, cancellationToken);
        }
    }
}
