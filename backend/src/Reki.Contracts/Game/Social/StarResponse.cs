using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record StarResponse(
    string Id,
    string Name,
    string Role,
    string Trait,
    string About,
    int Relation);

