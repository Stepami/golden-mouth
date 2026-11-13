using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.LikePostCommand;

public sealed record LikePost(PostId PostId, WorldId WorldId, AuthorId AuthorId) : ICommand;
