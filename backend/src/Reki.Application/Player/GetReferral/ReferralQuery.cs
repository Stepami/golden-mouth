using Mediator;
using Reki.Application.Player.Responses;
using Reki.Domain.Player;

namespace Reki.Application.Player.GetReferral;

public sealed record ReferralQuery(PlayerId PlayerId) : IQuery<ReferralResponse>;