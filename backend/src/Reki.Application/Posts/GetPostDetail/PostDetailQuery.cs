using Mediator;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GetPostDetail;

public sealed record PostDetailQuery(PostId PostId, WorldId WorldId) : IQuery<PostDetailResponse?>;