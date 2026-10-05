namespace Reki.Application.Posts;

public sealed record StarDirectMessageContext(
    string StarId,
    string World,
    string Voice,
    IReadOnlyList<string> Memory,
    string Text);