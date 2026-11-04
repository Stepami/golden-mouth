using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

public sealed record OnboardPlayer(
    TelegramUserId TelegramUserId,
    string BirthGroup,
    Address Address,
    string? ReferralCode,
    string? AgeBand) : ICommand<PlayerProfile>;
