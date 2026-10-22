using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record OnboardPlayer(
    TelegramUserId TelegramUserId,
    string BirthGroup,
    string Address,
    string? ReferralCode,
    string? AgeBand) : IRequest<PlayerProfile>;