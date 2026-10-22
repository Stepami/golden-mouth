namespace Reki.Application.Social.Responses;

public sealed record ChallengeResponse(
    string Id,
    string Me,
    FriendResponse Rival,
    int Day,
    int Days,
    double MyScore,
    double RivalScore,
    string BestPost);

