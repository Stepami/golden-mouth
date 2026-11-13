namespace Reki.Domain.Worlds;

public sealed record WorldStar(
    StarId Id,
    string Name,
    string Role,
    StarTrait Trait,
    string About,
    string Voice,
    string AvatarSeed)
{
    public static readonly WorldStar DeletedAccount = new(
        StarId.From("deleted"),
        "Удалённый аккаунт",
        string.Empty,
        default,
        string.Empty,
        string.Empty,
        string.Empty);
}
