using Mediator;
using Reki.Domain.Game.Social;

namespace Reki.Application.Game.Social;

public sealed record GetActiveChallengeQuery(long TelegramUserId) : IRequest<GameChallenge?>;

