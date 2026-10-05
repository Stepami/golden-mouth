using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record ChallengeView(
    PlayerId PlayerId,
    GameChallenge Challenge,
    PlayerProfile Rival,
    int Day);
