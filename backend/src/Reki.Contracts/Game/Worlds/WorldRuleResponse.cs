using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record WorldRuleResponse(string Text, bool Locked = false);

