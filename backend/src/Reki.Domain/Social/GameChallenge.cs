using Reki.Domain.Player;

namespace Reki.Domain.Social;

public sealed record GameChallenge(
    ChallengeId Id,
    PlayerId PlayerId,
    PlayerId FriendId,
    int Day,
    int Days,
    int MyScore,
    int RivalScore,
    string BestPost);

