using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetPostQuery(PostId PostId)
    : IRequest<PostDetailResponse>;
