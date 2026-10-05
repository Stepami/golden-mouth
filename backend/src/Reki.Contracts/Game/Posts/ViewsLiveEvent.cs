using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record ViewsLiveEvent(LiveEventType Type, long Views);

