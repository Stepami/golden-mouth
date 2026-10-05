using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record GetActiveChallengeQuery(TelegramUserId TelegramUserId) : IRequest<GameChallenge?>;

