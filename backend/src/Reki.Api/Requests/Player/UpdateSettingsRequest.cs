using System.ComponentModel.DataAnnotations;
using Reki.Domain.Player;

namespace Reki.Api.Requests.Player;

public sealed record UpdateSettingsRequest
{
    public Address? Address { get; init; }

    [MaxLength(64)]
    public string? Notifications { get; init; }

    [MaxLength(64)]
    public string? Quiet { get; init; }

    public bool? Plus { get; init; }
}

