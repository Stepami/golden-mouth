using System.ComponentModel.DataAnnotations;
using Reki.Contracts.Player;

namespace Reki.Contracts.Worlds;

public sealed record CreateWorldRequest
{
    [Required]
    [RegularExpression("^(stage|academy|it|stream|own|evening|sorcerers|houses)$")]
    public required string BalanceKey { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Name { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Avatar { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Role { get; init; }

    [MaxLength(120)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(64)]
    public string AvatarStyle { get; init; } = "lorelei";

    [MaxLength(200)]
    public string Voice { get; init; } = "default";

    [EnumDataType(typeof(Drama))]
    public Drama Drama { get; init; } = Drama.Mid;

    public string Where { get; init; } = string.Empty;

    public string FirstContext { get; init; } = string.Empty;

    public string FirstPrompt { get; init; } = string.Empty;

    public string Emoji { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    [Range(1, 3)]
    public int Difficulty { get; init; } = 1;

    public string? UnlockSkill { get; init; }

    public int? UnlockLevel { get; init; }

    public IReadOnlyList<string> Voices { get; init; } = [];

    public IReadOnlyList<string> PostIdeas { get; init; } = [];

    public IReadOnlyList<string> DemoPosts { get; init; } = [];

    public IReadOnlyList<WantResponse> Wants { get; init; } = [];

    public IReadOnlyList<CollectionCardResponse> Achievements { get; init; } = [];

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<WorldRuleResponse> Rules { get; init; } = [];

    public IReadOnlyList<WorldStarResponse> Stars { get; init; } = [];
}