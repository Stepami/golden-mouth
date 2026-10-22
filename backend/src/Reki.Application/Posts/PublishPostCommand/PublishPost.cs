using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Domain.Worlds;
using LiveEvent = Reki.Application.Posts.Responses.LiveEvent.LiveEvent;

namespace Reki.Application.Posts.PublishPostCommand;

public sealed record PublishPost(string PublicationId, WorldId WorldId) : ICommand<IReadOnlyList<LiveEvent>>;