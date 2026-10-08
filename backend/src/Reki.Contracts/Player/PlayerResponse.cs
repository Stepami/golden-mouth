namespace Reki.Contracts.Player;

public sealed record PlayerResponse(
    string Id,
    string Name,
    string Handle,
    string Role,
    Address Address,
    WorldId World,
    long Subs,
    int PostsLeft,
    int PostsCap,
    int FirstPostInviteBonus,
    StreakResponse Streak,
    IReadOnlyDictionary<SkillId, SkillResponse> Skills,
    IReadOnlyList<WantResponse> Wants,
    IReadOnlyList<CollectionCardResponse> Collection,
    string Bio,
    string Type,
    NextWorldResponse? NextWorld,
    PlayerSettingsResponse Settings,
    IReadOnlyDictionary<string, string> Experiments);
