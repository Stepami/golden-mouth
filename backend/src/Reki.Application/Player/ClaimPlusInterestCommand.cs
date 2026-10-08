using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record ClaimPlusInterestCommand(TelegramUserId TelegramUserId, bool Interest) : IRequest<int>;
