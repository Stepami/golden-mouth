using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record StoryCardData(
    PostId PostId,
    string PlayerName,
    string Handle,
    long Subscribers,
    string PostText,
    long Views,
    long SubscriberDelta,
    bool Viral)
{
    public static StoryCardData From(Reki.Domain.Worlds.World player, GamePost post) => new(
        post.Id,
        player.Player.Name,
        player.Player.Avatar,
        player.Player.Subscribers,
        post.Text,
        post.Views,
        post.SubscriberDelta,
        post.Viral);
}