using Mediator;

namespace Reki.Application.Player;

public sealed record DeletePlayerCommand(long TelegramUserId) : IRequest;

