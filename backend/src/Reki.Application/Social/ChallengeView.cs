using Reki.Domain.Player;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record ChallengeView(
    PlayerId PlayerId,
    GameChallenge Challenge,
    PlayerProfile Rival,
    World RivalWorld,
    int Day);