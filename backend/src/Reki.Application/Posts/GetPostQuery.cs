using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetPostQuery(TelegramUserId TelegramUserId, PostId PostId)
    : IRequest<PostDetailResponse>;
