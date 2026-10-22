namespace Reki.Application.Social.Responses;

public sealed record DmResponse(
    StarResponse Star,
    string Status,
    IReadOnlyList<string> Memory,
    IReadOnlyList<DirectMessageResponse> Messages,
    IReadOnlyList<string> Ideas,
    int RepliesLeft);

