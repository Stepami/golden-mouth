using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record CommentLiveEvent(LiveEventType Type, CommentResponse Comment);

