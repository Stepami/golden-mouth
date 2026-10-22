using Mediator;

namespace Reki.Application.Player.ClaimPlusInterestCommand;

public sealed record ClaimPlusInterest(bool Interest) : ICommand<int>;
