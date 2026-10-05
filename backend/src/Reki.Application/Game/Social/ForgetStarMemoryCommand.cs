using Mediator;

namespace Reki.Application.Game.Social;

public sealed record ForgetStarMemoryCommand(long TelegramUserId, string StarId, string Fact) : IRequest;

