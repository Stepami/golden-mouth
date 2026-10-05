using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record DmResponse(
    StarResponse Star,
    string Status,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessageResponse> Messages,
    IReadOnlyList<string> Ideas,
    int RepliesLeft);

