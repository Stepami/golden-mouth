using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostStateComparer : IStateComparer<CreatePost, CreatePostResponse>
{
    public ValueTask<Func<CreatePostResponse, CancellationToken, ValueTask>> CaptureAsync(
        CreatePost command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var worldId = command.WorldId;
        if (!player.DailyChallenge.Values.ContainsKey(worldId))
        {
            return ValueTask.FromResult<Func<CreatePostResponse, CancellationToken, ValueTask>>(
                static (_, _) => ValueTask.CompletedTask);
        }

        return ValueTask.FromResult<Func<CreatePostResponse, CancellationToken, ValueTask>>(
            (response, _) =>
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

                player.ApplyDailyChallengeProgress(worldId, changes);
                return ValueTask.CompletedTask;
            });
    }
}
