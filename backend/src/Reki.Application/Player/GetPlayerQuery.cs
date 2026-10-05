using Mediator;

namespace Reki.Application.Player;

public sealed record GetPlayerQuery(long TelegramUserId) : IRequest<PlayerView>;

