using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts.GetPublication;

public sealed record PublicationQuery(string PublicationId, PlayerId? PlayerId = null) : IQuery<Publication?>;
