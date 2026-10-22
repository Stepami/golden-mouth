namespace Reki.Api.Requests.Worlds;

public sealed record WantRequest(string Id, string Text, string Reward, bool Done);

