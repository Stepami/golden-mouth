using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record RibbonResponse(string Icon, string Title, string? Sub);

