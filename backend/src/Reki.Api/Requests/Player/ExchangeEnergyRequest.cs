using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Reki.Api.Requests.Player;

/// <summary>Idempotent crystal exchange using the version from the latest wallet snapshot.</summary>
public sealed record ExchangeEnergyRequest(
    [Required, MaxLength(64)] string RequestId,
    [property: JsonRequired]
    [Range(0, int.MaxValue)] int StateVersion);
