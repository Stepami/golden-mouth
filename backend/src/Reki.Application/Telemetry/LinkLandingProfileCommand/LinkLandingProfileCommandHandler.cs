using Mediator;
using Reki.Application.RekiContext;

namespace Reki.Application.Telemetry.LinkLandingProfileCommand;

internal sealed class LinkLandingProfileCommandHandler(
    LandingTelemetry telemetry, IRekiContextAccessor contextAccessor)
    : ICommandHandler<LinkLandingProfile>
{
    public async ValueTask<Unit> Handle(LinkLandingProfile command, CancellationToken cancellationToken)
    {
        var player = await contextAccessor.Context.Player;
        telemetry.LinkProfile(player, command.Visit);
        return Unit.Value;
    }
}
