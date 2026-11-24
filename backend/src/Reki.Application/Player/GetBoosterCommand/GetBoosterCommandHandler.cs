using Mediator;
using Reki.Application.RekiContext;
using Reki.Domain.Bonuses;

namespace Reki.Application.Player.GetBoosterCommand;

internal sealed class GetBoosterCommandHandler(
    IRekiContextAccessor rekiContextAccessor,
    IUnitOfWork unitOfWork,
    IPlayerRepository playerRepository,
    IBonusRepository bonusRepository) : ICommandHandler<GetBooster, bool>
{
    public async ValueTask<bool> Handle(GetBooster command, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(
            async token =>
            {
                var player = await rekiContextAccessor.Context.Player;
                if (!player.TrySpendCrystals(6))
                {
                    return false;
                }

                await playerRepository.ReplacePlayerAsync(player, token);
                await bonusRepository.InsertBonusesAsync(
                    [
                        new ViralCardBonus(BonusId.New(), player.Id, 3),
                        new ViralCardBonus(BonusId.New(), player.Id, 3),
                        new ViralCardBonus(BonusId.New(), player.Id, 3),
                        new FlopGuardBonus(BonusId.New(), player.Id, 5),
                        new FlopGuardBonus(BonusId.New(), player.Id, 5),
                        new FlopGuardBonus(BonusId.New(), player.Id, 5),
                    ],
                    token);
                return true;
            },
            cancellationToken);
    }
}
