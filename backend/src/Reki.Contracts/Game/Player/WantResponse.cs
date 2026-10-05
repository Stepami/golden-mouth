namespace Reki.Contracts.Game.Player;

public sealed record WantResponse(string Id, string Text, string Reward, bool Done);

