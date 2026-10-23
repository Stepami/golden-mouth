using Reki.Domain.Events;

namespace Reki.Application.Events.Responses;

public sealed record GameEventResponse(
    RekiEventId Id,
    GameEventKind Kind,
    string Badge,
    string Title,
    string Text,
    IReadOnlyList<string> Ideas,
    string Reward,
    string Note);

