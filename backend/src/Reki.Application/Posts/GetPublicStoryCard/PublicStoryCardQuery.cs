using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.GetPublicStoryCard;

public sealed record PublicStoryCardQuery(PlayerId PlayerId, PostId PostId)
    : IQuery<StoryCardData>;
