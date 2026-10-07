using Reki.Domain.Player;
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
    public static StoryCardData From(PlayerProfile player, GamePost post) => new(
        post.Id,
        player.Name,
        player.Handle,
        player.Subscribers,
        post.Text,
        post.Views,
        post.SubscriberDelta,
        post.Viral);
}
