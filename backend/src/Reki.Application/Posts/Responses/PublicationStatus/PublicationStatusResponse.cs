using Reki.Application.Posts.Responses.PostResult;
using PublicationStatus = Reki.Application.Posts.PublicationStatus;

namespace Reki.Application.Posts.Responses.PublicationStatus;

/// <summary>The durable status of a publication for recovery after a broken stream.</summary>
public sealed record PublicationStatusResponse(
    string PublicationId,
    Posts.PublicationStatus Status,
    PostResultResponse? Result,
    PublicationFailure? Failure = null);
