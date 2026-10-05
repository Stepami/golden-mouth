using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ReactionLiveEvent(LiveEventType Type, string Emoji);

