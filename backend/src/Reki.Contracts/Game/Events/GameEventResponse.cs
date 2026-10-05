using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record GameEventResponse(
    string Id,
    GameEventKind Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note);

