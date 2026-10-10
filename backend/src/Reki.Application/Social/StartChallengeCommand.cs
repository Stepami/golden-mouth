using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Social;

public sealed record StartChallengeCommand(PlayerId FriendId) : IRequest;

