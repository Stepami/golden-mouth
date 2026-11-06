using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.GetPublication;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Application.RekiContext;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.PublishPostCommand;

internal sealed class PublishPostCommandHandler(
    IPublicationRepository publicationRepository,
    IOptionsMonitor<GameBalance> balance,
    PostPublisher publisher,
    TimeProvider timeProvider,
    ISender sender,
    IRekiContextAccessor rekiContextAccessor,
    PlayerSafetyGuard safety,
    StarTurnPolicy turnPolicy) : ICommandHandler<PublishPost, IReadOnlyList<LiveEvent>>
{
    public async ValueTask<IReadOnlyList<LiveEvent>> Handle(
        PublishPost request,
        CancellationToken cancellationToken)
    {
        var publication = await sender.Send(new PublicationQuery(request.PublicationId), cancellationToken);
        var storedPlayer = await rekiContextAccessor.Context.Player;
        var worlds = await rekiContextAccessor.Context.Worlds;
        var world = worlds.GetValueOrDefault(request.WorldId)
            ?? throw new ResourceNotFoundException("World not found.");
        if (publication is null || publication.PlayerId != storedPlayer.Id || publication.WorldId != world.Id)
        {
            throw new ResourceNotFoundException("Publication not found.");
        }

        if (publication is { Status: PublicationStatusType.Completed, Result: not null })
        {
            return publication.Result.MapLiveEvents(world, storedPlayer.Id);
        }

        if (publication.Status != PublicationStatusType.ReadyForModeration)
        {
            throw new RequestConflictException("The publication is not ready for moderation.");
        }

        var currentBalance = balance.CurrentValue;
        var text = publication.Text;
        var ownWords = publication.OwnWords;
        var wantId = publication.WantId;
        var now = timeProvider.GetUtcNow();
        var player = storedPlayer;

        if (player.Energy < currentBalance.PostEnergyCost)
        {
            throw new RequestConflictException("The daily post limit has been reached.");
        }

        var turn = publication.AssembleTurn();
        try
        {
            await safety.EnsureInputAllowedAsync(player, [publication.Text], cancellationToken);
            PlayerSafetyGuard.EnsureOutputAllowed([turn.Comment, turn.Hook]);
        }
        catch (Exception exception) when (exception is ContentRejectedException or UnsafeGeneratedContentException)
        {
            var category = exception is ContentRejectedException rejected ? rejected.Category : ((UnsafeGeneratedContentException)exception).Category;
            var failure = new PublicationFailure(exception is ContentRejectedException ? "content_rejected" : "unsafe_generated_content", category, PublicationSafetyPolicy.Version);
            await publicationRepository.MarkPublicationFailedAsync(publication.Id, now.AddDays(1).UtcDateTime, failure, cancellationToken);
            throw;
        }

        turnPolicy.EnsurePostAllowed(turn, player, world);

        var wantCompleted = wantId is not null && world.Player.Wants.Any(want =>
            want.Id == wantId && !want.Done);
        if (wantId is not null && !wantCompleted)
        {
            throw new RequestConflictException("The selected daily want is unavailable.");
        }

        var post = new GamePost(
            PostId.New(now),
            player.Id.ToAuthorId(),
            world.Id,
            text,
            0,
            [new GameComment(CommentId.New(), null, null, turn.StarId.ToAuthorId(), turn.Comment, false, [])],
            now,
            0,
            0,
            1,
            false,
            false)
        {
            OwnWords = ownWords,
            WantId = wantId,
        };
        var published = await publisher.PublishAsync(post, player, world, turn, publication.Id, cancellationToken)
            ?? throw new RequestConflictException("The player state changed while publishing the post.");
        return published.MapLiveEvents(world, player.Id);
    }
}
