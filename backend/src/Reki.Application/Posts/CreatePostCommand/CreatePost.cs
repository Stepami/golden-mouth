using Mediator;
using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

public sealed record CreatePost(string Text, bool OwnWords, WorldId WorldId, PostId? RepostId = null) : ICommand<CreatePostResponse>, ITrackDailyProgress;
