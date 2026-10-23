namespace Reki.Application.Worlds.Responses;

public sealed record WorldStarResponse(
    string Id,
    string Name,
    string Role,
    int Trait,
    string About,
    string Voice,
    string AvatarSeed,
    int Relation);
