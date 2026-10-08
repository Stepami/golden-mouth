using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record ClaimPlusInterestCommand(TelegramUserId TelegramUserId) : IRequest<int>
{
    public bool Interest { get; init; } = true;
}

