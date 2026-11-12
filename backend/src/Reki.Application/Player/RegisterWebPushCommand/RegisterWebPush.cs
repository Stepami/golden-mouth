using Mediator;

namespace Reki.Application.Player.RegisterWebPushCommand;

public sealed record RegisterWebPush(string Endpoint, string P256dh, string Auth) : ICommand;