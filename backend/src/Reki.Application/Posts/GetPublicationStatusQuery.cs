using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record GetPublicationStatusQuery(string PublicationId) : IRequest<PublicationStatusResponse>;