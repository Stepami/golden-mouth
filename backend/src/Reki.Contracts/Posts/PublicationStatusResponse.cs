namespace Reki.Contracts.Posts;

/// <summary>The durable status of a publication for recovery after a broken stream.</summary>
public sealed record PublicationStatusResponse(
    string PublicationId,
    PublicationStatus Status,
    PostResultResponse? Result,
    PublicationFailure? Failure = null);
