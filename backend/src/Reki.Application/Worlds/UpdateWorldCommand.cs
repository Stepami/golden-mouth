using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record UpdateWorldCommand(
    string? Title,
    string? Description,
    IReadOnlyList<WorldRule>? Rules,
    WorldDrama? Drama,
    string? Voice,
    WorldId WorldId) : IRequest;