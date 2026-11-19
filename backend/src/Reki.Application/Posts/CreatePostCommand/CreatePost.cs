using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Application.Telemetry;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

public sealed record CreatePost(string Text, bool OwnWords, WorldId WorldId, PostId? RepostId = null) : ICommand<ProductGoalResult<CreatePostResponse>>;
