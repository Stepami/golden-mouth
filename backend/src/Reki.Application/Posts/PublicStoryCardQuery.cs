using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record PublicStoryCardQuery(PlayerId PlayerId, PostId PostId)
    : IRequest<StoryCardData>;
