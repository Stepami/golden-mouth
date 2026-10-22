namespace Reki.Application.Player.Responses;

public sealed record StreakResponse(int Days, IReadOnlyList<bool> Week, string NextReward);

