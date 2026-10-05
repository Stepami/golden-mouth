using Mediator;

namespace Reki.Application.Social;

public sealed record ForgetStarMemoryCommand(long TelegramUserId, string StarId, string Fact) : IRequest;

