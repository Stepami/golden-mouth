namespace Reki.Contracts.Game.Events;

public sealed record GameEventResponse(
    string Id,
    GameEventKind Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note);

