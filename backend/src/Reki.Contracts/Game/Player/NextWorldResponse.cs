using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record NextWorldResponse(string Id, string Title, string Need);

