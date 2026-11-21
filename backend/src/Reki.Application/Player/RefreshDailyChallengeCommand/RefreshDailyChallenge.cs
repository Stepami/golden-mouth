using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.RefreshDailyChallengeCommand;

public sealed record RefreshDailyChallenge(PlayerProfile Player) : ICommand<PlayerProfile>;
