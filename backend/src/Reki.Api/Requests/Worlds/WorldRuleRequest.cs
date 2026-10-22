namespace Reki.Api.Requests.Worlds;

public sealed record WorldRuleRequest(string Text, bool Locked = false);

