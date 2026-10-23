using Reki.Domain.Worlds;

namespace Reki.Application.Social.Responses.Dm;

public sealed record StarResponse(
    StarId Id,
    string Name,
    string Role,
    string Trait,
    string About,
    int Relation);

