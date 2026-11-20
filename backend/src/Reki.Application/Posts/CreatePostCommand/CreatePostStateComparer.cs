using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses;
using Reki.Application.RekiContext;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostStateComparer(DailyProgress progress) : IStateComparer<CreatePost, CreatePostResponse>
{
    public ValueTask<Func<CreatePostResponse, CancellationToken, ValueTask>?> CaptureAsync(
        CreatePost command,
        IRekiContext context,
        PlayerChallenge challenge,
        CancellationToken cancellationToken)
    {
        var worldId = command.WorldId;
        if (!challenge.Values.ContainsKey(worldId))
        {
            return default;
        }

        var playerId = context.PlayerId;
        var day = challenge.Day;

        return ValueTask.FromResult<Func<CreatePostResponse, CancellationToken, ValueTask>?>(
            (response, token) =>
            {
                var isPost = response.Post.RepostId is null;
                var delta = response.Result.Delta;
                var changes = new Dictionary<DailyTaskType, long>
                {
                    [isPost ? DailyTaskType.Posts : DailyTaskType.Reposts] = 1,
                };
                if (delta != 0)
                {
                    changes[delta > 0 ? DailyTaskType.Subscribers : DailyTaskType.Unsubscribers] = Math.Abs(delta);
                }

                if (isPost)
                {
                    changes[DailyTaskType.PostViews] = response.Result.Views;
                    changes[DailyTaskType.PostLikes] = response.Post.Likes;
                }

                return progress.ApplyAsync(playerId, day, worldId, changes, token);
            });
    }
}
