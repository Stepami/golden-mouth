using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record PlayerSettingsResponse(
    Drama Drama,
    string Notifications,
    string Quiet,
    string Voice,
    bool Plus);

