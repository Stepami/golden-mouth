using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.Streaks.UpdateStreakCommand;

public sealed record UpdateStreak(PlayerId PlayerId, DateTimeOffset Now) : ICommand<bool>;
