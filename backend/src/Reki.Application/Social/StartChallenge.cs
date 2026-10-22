using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Social;

public sealed record StartChallenge(PlayerId FriendId) : ICommand;

