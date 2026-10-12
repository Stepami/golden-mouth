using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GetPostDetailQuery(PostId PostId, WorldId WorldId) : IRequest<PostDetailResponse>;