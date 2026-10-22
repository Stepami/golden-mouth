using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.GetActiveChallenge;

public sealed record ChallengeView(
    PlayerId PlayerId,
    GameChallenge Challenge,
    PlayerProfile Rival,
    World RivalWorld,
    int Day);