namespace Reki.Contracts.Social;

public sealed record ChallengeResponse(
    string Id,
    string Me,
    FriendResponse Rival,
    int Day,
    int Days,
    int MyScore,
    int RivalScore,
    string BestPost);

