using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts.GetPublicationStatus;

public sealed record PublicationStatusQuery(string PublicationId) : IQuery<PublicationStatusResponse>;