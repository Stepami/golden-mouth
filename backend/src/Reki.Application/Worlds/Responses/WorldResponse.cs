using Reki.Domain.Worlds;

namespace Reki.Application.Worlds.Responses;

public sealed record WorldResponse
{
    public required WorldId Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string AvatarStyle { get; init; }

    public required string Emoji { get; init; }

    public required string Summary { get; init; }

    public WorldDifficulty Difficulty { get; init; } = WorldDifficulty.Easy;

    public string? UnlockSkill { get; init; }

    public int? UnlockLevel { get; init; }

    public required string Where { get; init; }

    public required string FirstContext { get; init; }

    public required string FirstPrompt { get; init; }

    public required IReadOnlyList<string> Roles { get; init; }

    public required IReadOnlyList<string> Voices { get; init; }

    public required IReadOnlyList<string> PostIdeas { get; init; }

    public required IReadOnlyList<string> DemoPosts { get; init; }

    public required IReadOnlyList<WorldRuleResponse> Rules { get; init; }

    public required IReadOnlyList<WorldStarResponse> Stars { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public required string Avatar { get; init; }

    public required string Voice { get; init; }

    public required WorldDrama Drama { get; init; }

    public long Subscribers { get; init; }

    public required IReadOnlyList<CollectionCardResponse> Achievements { get; init; }
}
