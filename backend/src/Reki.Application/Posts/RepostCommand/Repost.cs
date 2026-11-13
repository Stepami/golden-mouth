using Mediator;
using Reki.Application.Posts.Responses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.RepostCommand;

public sealed record Repost(PostId PostId, WorldId WorldId, bool Active) : ICommand<RepostResponse>;
