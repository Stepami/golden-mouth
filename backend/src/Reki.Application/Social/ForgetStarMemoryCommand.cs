using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record ForgetStarMemoryCommand(TelegramUserId TelegramUserId, StarId StarId, string Fact) : IRequest;

