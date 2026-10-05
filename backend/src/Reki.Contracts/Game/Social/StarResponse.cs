namespace Reki.Contracts.Game.Social;

public sealed record StarResponse(
    string Id,
    string Name,
    string Role,
    string Trait,
    string About,
    int Relation);

