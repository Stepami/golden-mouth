using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GetPublicationQuery(string PublicationId, PlayerId? PlayerId = null) : IQuery<Publication?>;
