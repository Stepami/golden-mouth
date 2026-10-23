namespace Reki.Domain.Worlds;

public sealed record WorldInterface
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string AvatarStyle { get; init; } = string.Empty;

    public string Where { get; init; } = string.Empty;

    public string FirstContext { get; init; } = string.Empty;

    public string FirstPrompt { get; init; } = string.Empty;

    public string Emoji { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string? UnlockSkill { get; init; }

    public int? UnlockLevel { get; init; }
}
