using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record DeletePlayerCommand(long TelegramUserId) : IRequest;

