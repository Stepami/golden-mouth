using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

public sealed record CreatePost(WorldId WorldId, string RequestId, string Text, bool OwnWords, string? WantId) : ICommand<CreatePostResponse>;
