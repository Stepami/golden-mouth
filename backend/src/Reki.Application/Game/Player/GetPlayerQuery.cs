using Mediator;

namespace Reki.Application.Game.Player;

public sealed record GetPlayerQuery(long TelegramUserId) : IRequest<PlayerView>;

