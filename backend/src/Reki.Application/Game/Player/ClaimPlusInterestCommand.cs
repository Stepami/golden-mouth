using Mediator;

namespace Reki.Application.Game.Player;

public sealed record ClaimPlusInterestCommand(long TelegramUserId) : IRequest<int>;

