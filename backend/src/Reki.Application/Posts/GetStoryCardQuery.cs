using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetStoryCardQuery(TelegramUserId TelegramUserId, PostId PostId)
    : IRequest<StoryCardData>;
