namespace Reki.Contracts.Game.Player;

public sealed record StreakResponse(int Days, IReadOnlyList<bool> Week, string NextReward);

