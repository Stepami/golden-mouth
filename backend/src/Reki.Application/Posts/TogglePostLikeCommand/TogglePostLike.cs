using Mediator;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.TogglePostLikeCommand;

public sealed record TogglePostLike(PostId PostId, WorldId WorldId, AuthorId AuthorId) : ICommand<PostDetailResponse>;
