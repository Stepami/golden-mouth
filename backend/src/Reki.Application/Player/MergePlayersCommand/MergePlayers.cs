using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player.MergePlayersCommand;

public sealed record MergePlayers(PlayerId FromPlayerId, PlayerId ToPlayerId) : ICommand;
