using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.LikePostCommand;

public sealed record LikePost(PostId PostId, AuthorId AuthorId) : ICommand;
