namespace Reki.Contracts.Worlds;

public sealed record WorldStarResponse(string Id, string Name, string Role, int Trait, string About, string Voice, string AvatarSeed, int Relation)
{
    public string AppearanceProfile { get; init; } = "neutral";

    public string AgeBand { get; init; } = string.Empty;
}
