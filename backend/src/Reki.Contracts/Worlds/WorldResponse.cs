using System.ComponentModel.DataAnnotations;
using Reki.Contracts.Player;

namespace Reki.Contracts.Worlds;

public sealed record WorldResponse
{
    public required string Id { get; init; }

    public required string BalanceKey { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string AvatarStyle { get; init; }

    public required string Emoji { get; init; }

    public required string Summary { get; init; }

    [Range(1, 3)]
    public int Difficulty { get; init; } = 1;

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

    public required Drama Drama { get; init; }

    public long Subscribers { get; init; }

    public required IReadOnlyList<WantResponse> Wants { get; init; }

    public required IReadOnlyList<CollectionCardResponse> Achievements { get; init; }
}