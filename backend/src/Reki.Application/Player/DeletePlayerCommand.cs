using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record DeletePlayerCommand(TelegramUserId TelegramUserId) : IRequest;

