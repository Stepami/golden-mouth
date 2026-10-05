using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record ClaimPlusInterestCommand(long TelegramUserId) : IRequest<int>;

