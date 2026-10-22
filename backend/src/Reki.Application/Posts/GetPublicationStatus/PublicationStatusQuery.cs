using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.PublicationStatus;

namespace Reki.Application.Posts.GetPublicationStatus;

public sealed record PublicationStatusQuery(string PublicationId) : IQuery<PublicationStatusResponse>;