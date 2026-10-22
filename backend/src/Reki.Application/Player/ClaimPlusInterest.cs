using Mediator;

namespace Reki.Application.Player;

public sealed record ClaimPlusInterest(bool Interest) : ICommand<int>;
