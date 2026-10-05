using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record WantResponse(string Id, string Text, string Reward, bool Done);

