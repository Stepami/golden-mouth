using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record SendDirectMessageResponse(string? Reply, int RepliesLeft);

