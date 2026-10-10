using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record GetPublicationQuery(string PublicationId) : IQuery<PublicationStatusResponse>;
