namespace Reki.Api.Requests.Worlds;

public sealed record WorldStarRequest(
    string Id,
    string Name,
    string Role,
    int Trait,
    string About,
    string Voice,
    string AvatarSeed,
    int Relation);
