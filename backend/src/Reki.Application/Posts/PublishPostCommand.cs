using Mediator;
using Reki.Contracts.Posts;

namespace Reki.Application.Posts;

public sealed record PublishPostCommand(string PublicationId) : IStreamRequest<LiveEvent>;

