namespace Reki.Application.Posts;

public sealed record StoryCardData(
    string PostId,
    string PlayerName,
    string Handle,
    long Subscribers,
    string PostText,
    long Views,
    long SubscriberDelta,
    bool Viral);
