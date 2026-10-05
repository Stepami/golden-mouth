using Mediator;

namespace Reki.Application.Posts;

public sealed record GetStoryCardQuery(long TelegramUserId, string PostId)
    : IRequest<StoryCardData>;
