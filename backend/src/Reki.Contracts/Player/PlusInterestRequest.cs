namespace Reki.Contracts.Player;

public sealed record PlusInterestRequest
{
    public bool Interest { get; init; } = true;
}
