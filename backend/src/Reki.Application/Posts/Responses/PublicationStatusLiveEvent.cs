namespace Reki.Application.Posts.Responses;

/// <summary>A generation-stage status delivered over SSE.</summary>
public sealed record PublicationStatusLiveEvent(
    PublicationStatus Type,
    string PublicationId,
    PublicationFailure? Failure = null);
