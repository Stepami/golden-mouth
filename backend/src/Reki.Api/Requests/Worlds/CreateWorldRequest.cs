using System.ComponentModel.DataAnnotations;
using Reki.Domain.Worlds;

namespace Reki.Api.Requests.Worlds;

public sealed record CreateWorldRequest
{
    public WorldId? TemplateId { get; init; }

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

    [EnumDataType(typeof(WorldDrama))]
    public WorldDrama Drama { get; init; } = WorldDrama.Mid;

    public string Where { get; init; } = string.Empty;

    public string FirstContext { get; init; } = string.Empty;

    public string FirstPrompt { get; init; } = string.Empty;

    public string Emoji { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public WorldDifficulty Difficulty { get; init; } = WorldDifficulty.Normal;

    public string? UnlockSkill { get; init; }

    public int? UnlockLevel { get; init; }

    public IReadOnlyList<string> Voices { get; init; } = [];

    public IReadOnlyList<string> PostIdeas { get; init; } = [];

    public IReadOnlyList<string> DemoPosts { get; init; } = [];

    public IReadOnlyList<WantRequest> Wants { get; init; } = [];

    public IReadOnlyList<CollectionCardRequest> Achievements { get; init; } = [];

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<WorldRuleRequest> Rules { get; init; } = [];

    public IReadOnlyList<WorldStarRequest> Stars { get; init; } = [];
}