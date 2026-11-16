using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

public sealed record OnboardPlayer(
    ExternalUserId ExternalUserId,
    Address Address,
    string? ReferralCode,
    int BirthYear) : ICommand<PlayerProfile>;
