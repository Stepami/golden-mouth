using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record PublicationQuery(string PublicationId, PlayerId? PlayerId = null) : IQuery<Publication?>;
