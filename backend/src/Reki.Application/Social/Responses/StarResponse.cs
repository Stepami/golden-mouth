namespace Reki.Application.Social.Responses;

public sealed record StarResponse(
    string Id,
    string Name,
    string Role,
    string Trait,
    string About,
    int Relation);

