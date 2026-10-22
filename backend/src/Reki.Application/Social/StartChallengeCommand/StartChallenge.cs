using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Social.StartChallengeCommand;

public sealed record StartChallenge(PlayerId FriendId) : ICommand;

