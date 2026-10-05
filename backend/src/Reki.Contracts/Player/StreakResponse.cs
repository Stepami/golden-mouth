namespace Reki.Contracts.Player;

public sealed record StreakResponse(int Days, IReadOnlyList<bool> Week, string NextReward);

