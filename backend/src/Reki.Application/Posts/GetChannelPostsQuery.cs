using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GetChannelPostsQuery(TelegramUserId TelegramUserId)
    : IRequest<IReadOnlyList<ChannelPostResponse>>;
