using Mediator;
using Reki.Contracts.Player;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record GetReferralQuery(TelegramUserId TelegramUserId) : IRequest<ReferralResponse>;
