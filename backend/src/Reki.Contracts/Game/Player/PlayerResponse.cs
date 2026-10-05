using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record PlayerResponse(
    string Id,
    string Name,
    string Handle,
    string Role,
    string Avatar,
    Address Address,
    WorldId World,
    long Subs,
    int PostsLeft,
    int PostsCap,
    StreakResponse Streak,
    IReadOnlyDictionary<SkillId, SkillResponse> Skills,
    IReadOnlyList<WantResponse> Wants,
    IReadOnlyList<CollectionCardResponse> Collection,
    string Bio,
    string Type,
    NextWorldResponse? NextWorld,
    PlayerSettingsResponse Settings,
    IReadOnlyDictionary<string, string> Experiments);

