using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetActiveChallengeQuery(long TelegramUserId) : IRequest<GameChallenge?>;

