using Reki.Application.Posts.Responses.PostResult;

namespace Reki.Application.Posts.Responses.PublicationStatus;

/// <summary>The durable status of a publication for recovery after a broken stream.</summary>
public sealed record PublicationStatusResponse(
    string PublicationId,
    PublicationStatusType Status,
    PostResultResponse? Result,
    PublicationFailure? Failure = null);
