using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record PublishPost(string PublicationId, WorldId WorldId) : ICommand<IReadOnlyList<LiveEvent>>;