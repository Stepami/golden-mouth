using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record CollectionCardResponse(string Id, string Emoji, string Title, bool Open);

