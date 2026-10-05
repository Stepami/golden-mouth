using Mediator;

namespace Reki.Application.Game.Player;

public sealed record DeletePlayerCommand(long TelegramUserId) : IRequest;

