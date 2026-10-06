using System.Runtime.CompilerServices;
using Reki.Api.Mapping;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;

namespace Reki.Api.Posts;

public sealed class PostLiveEventMapper(ApiMapper mapper)
{
    public static string MapPublicationStatus(PublicationStatus status) => status switch
    {
        PublicationStatus.Pending => "pending",
        PublicationStatus.ReadyForModeration => "ready_for_moderation",
        PublicationStatus.Completed => "completed",
        PublicationStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static async IAsyncEnumerable<PublicationStatusLiveEvent> MapStatusesAsync(
        IAsyncEnumerable<PublicationStatus> statuses,
        string publicationId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (PublicationStatus status in statuses.WithCancellation(cancellationToken))
        {
            yield return new PublicationStatusLiveEvent(
                MapPublicationStatus(status),
                publicationId);
        }
    }

    public IAsyncEnumerable<object> MapPublishedAsync(
        PublishedPost published,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        GameComment comment = published.Post.Comments[0];
        object[] events =
        [
            new ViewsLiveEvent(LiveEventType.Views, Math.Min(48, published.Post.Views)),
            new ReactionLiveEvent(LiveEventType.React, published.Reaction),
            new CommentLiveEvent(LiveEventType.Comment, mapper.MapComment(comment)),
            new TypingLiveEvent(LiveEventType.Typing, published.StarName),
            new ResultLiveEvent(LiveEventType.Result, mapper.MapPublishedPost(published)),
        ];
        return events.ToAsyncEnumerable();
    }
}
