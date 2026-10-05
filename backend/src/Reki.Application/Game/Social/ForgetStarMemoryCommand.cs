using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record ForgetStarMemoryCommand(long TelegramUserId, string StarId, string Fact) : IRequest;

