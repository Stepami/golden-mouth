using Mediator;
using Reki.Contracts.Player;

namespace Reki.Application.Player;

public sealed record GetReferralQuery() : IRequest<ReferralResponse>;
