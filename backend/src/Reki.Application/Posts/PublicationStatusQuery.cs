using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record PublicationStatusQuery(string PublicationId) : IRequest<PublicationStatusResponse>;