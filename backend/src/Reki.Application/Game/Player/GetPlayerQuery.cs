using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetPlayerQuery(long TelegramUserId) : IRequest<PlayerView>;

