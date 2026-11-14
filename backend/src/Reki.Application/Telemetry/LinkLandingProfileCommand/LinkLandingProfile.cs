using Mediator;

namespace Reki.Application.Telemetry.LinkLandingProfileCommand;

public sealed record LinkLandingProfile(LandingVisit Visit) : ICommand;
