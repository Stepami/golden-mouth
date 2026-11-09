using Mediator;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Exceptions;
using Reki.Application.Invites;
using Reki.Application.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Application.Player.OnboardPlayerCommand;

internal sealed class OnboardPlayerCommandHandler(
    IUnitOfWork unitOfWork,
    IInvitationRepository invitationRepository,
    IPlayerRepository playerRepository,
    GameDayPeriod gameDayPeriod,
    IOptionsMonitor<GameBalance> balance,
    TimeProvider timeProvider,
    ProductTelemetry analytics) : ICommandHandler<OnboardPlayer, PlayerProfile>
{
    public async ValueTask<PlayerProfile> Handle(
        OnboardPlayer request,
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerAsync(request.PlayerId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player not found.");
        if (player.OnboardingCompleted)
        {
            throw new RequestConflictException("Onboarding has already been completed.");
        }

        var currentBalance = balance.CurrentValue;
        var inviter = await ResolveInviterAsync(request, player.Id, cancellationToken);
        var completed = player with
        {
            BirthGroup = request.BirthGroup,
            Address = request.Address,
            AgeBand = request.AgeBand,
            Energy = currentBalance.FreePostsPerDay,
            PostsPeriodStartedAtUtc = gameDayPeriod.PeriodStart,
            StreakDays = 1,
            Skills = new Dictionary<SkillId, SkillProgress>
            {
                [SkillId.From("brave")] = new(1, 0, 0),
                [SkillId.From("humor")] = new(1, 0, 0),
                [SkillId.From("topic")] = new(1, 0, 0),
            },
            OnboardingCompleted = true,
        };
        await unitOfWork.ExecuteAsync(
            async token =>
            {
                await playerRepository.ReplacePlayerAsync(completed, token);

                if (inviter is not null)
                {
                    var invite = Invitation.Create(inviter.Id, completed.Id, timeProvider.GetUtcNow(), currentBalance.InviteBonusBoth);
                    await invitationRepository.CreateInviteAsync(invite, token);
                }

                return true;
            },
            cancellationToken);

        analytics.TrackPlayer(completed, "onboarding_completed", new Dictionary<string, string>(), referred: inviter is not null);
        return completed;
    }

    private async ValueTask<PlayerProfile?> ResolveInviterAsync(
        OnboardPlayer request,
        PlayerId playerId,
        CancellationToken cancellationToken)
    {
        var code = request.ReferralCode;
        if (code is null || !code.StartsWith("ref_", StringComparison.Ordinal) ||
            !Guid.TryParseExact(code[4..], "N", out _))
        {
            return null;
        }

        var inviter = await playerRepository.GetPlayerAsync(PlayerId.From(code[4..]), cancellationToken);
        return inviter is not null && inviter.Id != playerId
            ? inviter
            : null;
    }
}
