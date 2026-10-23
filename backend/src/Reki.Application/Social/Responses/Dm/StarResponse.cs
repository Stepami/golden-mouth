namespace Reki.Application.Social.Responses.Dm;

public sealed record StarResponse(
    string Id,
    string Name,
    string Role,
    string Trait,
    string About,
    int Relation);

