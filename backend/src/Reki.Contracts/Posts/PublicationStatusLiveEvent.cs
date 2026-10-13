namespace Reki.Contracts.Posts;

/// <summary>A generation-stage status delivered over SSE.</summary>
public sealed record PublicationStatusLiveEvent(PublicationStatus Type, string PublicationId)
{
    public PublicationFailure? Failure { get; init; }
}
