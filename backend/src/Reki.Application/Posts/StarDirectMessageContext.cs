namespace Reki.Application.Posts;

public sealed record StarDirectMessageContext(
    StarAgentProfile Agent,
    string Text);
