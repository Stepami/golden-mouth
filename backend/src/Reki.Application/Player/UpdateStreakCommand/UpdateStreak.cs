using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.UpdateStreakCommand;

public sealed record UpdateStreak(PlayerProfile Player, DateTimeOffset Now) : ICommand;
