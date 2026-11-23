using Mediator;
using Reki.Application.Invites.Responses;
using Reki.Domain.Player;

namespace Reki.Application.Invites.GetReferral;

public sealed record ReferralQuery(PlayerId PlayerId) : IQuery<ReferralResponse>;