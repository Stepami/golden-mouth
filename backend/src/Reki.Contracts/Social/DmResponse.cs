namespace Reki.Contracts.Social;

public sealed record DmResponse(
    StarResponse Star,
    string Status,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessageResponse> Messages,
    IReadOnlyList<string> Ideas,
    int RepliesLeft);

