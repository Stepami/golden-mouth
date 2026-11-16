using Mediator;
using Reki.Application.Player.Responses;
using Reki.Domain.Player;

namespace Reki.Application.Player.Streaks.GetStreak;

public sealed record PlayerStreakQuery(PlayerId PlayerId) : IQuery<PlayerStreakResponse?>;
