using Mediator;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.PublishPostCommand;

public sealed record PublishPost(string PublicationId, WorldId WorldId) : ICommand<IReadOnlyList<LiveEvent>>;