using System.Text.Json;
using Reki.Application.Telemetry.SubmitAnalyticsCommand;
using Reki.Contracts.Events;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Domain.Events;
using Reki.Domain.Worlds;

namespace Reki.Api.Mapping;

public interface IApiMapper
{
    GameEventResponse MapEvent(DailyGameEvent source);

    AnalyticsSubmission MapAnalytics(AnalyticsBatchRequest source);

    WorldResponse MapWorld(World source);

    World MapWorldDefinition(CreateWorldRequest source);

    IReadOnlyDictionary<string, string> SanitizeProperties(
        IReadOnlyDictionary<string, JsonElement>? properties);
}
