using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record StreakResponse(int Days, IReadOnlyList<bool> Week, string NextReward);

