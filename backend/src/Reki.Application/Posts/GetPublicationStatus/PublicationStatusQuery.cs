using Mediator;
using Reki.Application.Posts.Responses;

namespace Reki.Application.Posts.GetPublicationStatus;

public sealed record PublicationStatusQuery(string PublicationId) : IQuery<PublicationStatusResponse>;