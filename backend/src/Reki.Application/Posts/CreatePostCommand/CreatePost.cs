using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

public sealed record CreatePost(string Text, bool OwnWords, string? WantId, WorldId WorldId, PostId? RepostId = null) : ICommand<CreatePostResponse>;
