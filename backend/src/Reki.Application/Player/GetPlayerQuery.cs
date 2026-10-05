using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record GetPlayerQuery(TelegramUserId TelegramUserId) : IRequest<PlayerView>;

