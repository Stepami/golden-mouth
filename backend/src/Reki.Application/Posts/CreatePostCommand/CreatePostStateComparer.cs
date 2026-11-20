using Reki.Application.Player.Challenges;
using Reki.Application.Posts.Responses;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.CreatePostCommand;

internal sealed class CreatePostStateComparer : IStateComparer<CreatePost, CreatePostResponse>
{
    public ValueTask<IStateComparer<CreatePost, CreatePostResponse>.IContinuation> CaptureAsync(
        CreatePost command,
        PlayerProfile player,
        CancellationToken cancellationToken)
    {
        var worldId = command.WorldId;
        if (!player.DailyChallenge.Values.ContainsKey(worldId))
        {
            return ValueTask.FromResult(EmptyStateComparerContinuation<CreatePost, CreatePostResponse>.Instance);
        }

        return ValueTask.FromResult<IStateComparer<CreatePost, CreatePostResponse>.IContinuation>(
            new PostContinuation(worldId));
    }

    private sealed class PostContinuation(WorldId worldId) : IStateComparer<CreatePost, CreatePostResponse>.IContinuation
    {
        public ValueTask<bool> ApplyAsync(
            CreatePostResponse response,
            PlayerProfile player,
            CancellationToken cancellationToken)
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

            var challengeChanged = player.ApplyDailyChallengeProgress(worldId, changes);
            return ValueTask.FromResult(challengeChanged);
        }
    }
}
