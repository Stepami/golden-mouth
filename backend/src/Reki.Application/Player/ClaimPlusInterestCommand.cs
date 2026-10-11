using Mediator;

namespace Reki.Application.Player;

public sealed record ClaimPlusInterestCommand(bool Interest) : IRequest<int>;
