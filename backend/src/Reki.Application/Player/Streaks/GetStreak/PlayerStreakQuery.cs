using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Player.Streaks;

namespace Reki.Application.Player.Streaks.GetStreak;

public sealed record PlayerStreakQuery(PlayerId PlayerId) : IQuery<PlayerStreak>;
