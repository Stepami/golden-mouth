using Mediator;

namespace Reki.Application.Player;

public sealed record ClaimPlusInterestCommand(long TelegramUserId) : IRequest<int>;

