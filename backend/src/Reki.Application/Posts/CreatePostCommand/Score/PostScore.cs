namespace Reki.Application.Posts.CreatePostCommand.Score;

internal sealed record PostScore(
    double Brave,
    double Humor,
    double Topic,
    bool PositiveEmotion,
    string? FlopReason);
