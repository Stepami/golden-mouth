namespace Reki.Domain.Game.Social;

public sealed record GameChallenge(
    string Id,
    string PlayerId,
    string FriendId,
    int Day,
    int Days,
    int MyScore,
    int RivalScore,
    string BestPost);

