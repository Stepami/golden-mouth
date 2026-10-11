using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record PublishPostCommand(string PublicationId, WorldId WorldId) : IStreamRequest<LiveEvent>;