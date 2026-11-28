using Mediator;
using Reki.Application.Player.Responses;
using Reki.Domain.Player;

namespace Reki.Application.Player.GetBoosterStock;

public sealed record BoosterStockQuery(PlayerId PlayerId) : IQuery<BoosterStockResponse>;
