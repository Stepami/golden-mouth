using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Contracts.Telemetry;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class TelemetryControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task ClientLogs_InDevelopment_AreNotWrittenToMongoAsync()
    {
        using var api = Factory.CreateApiClient(1003);
        await CreatePlayerWithWorldAsync(api);

        await api.Telemetry.SubmitClientLogsAsync(
            new ClientLogBatchRequest
            {
                Session = "test-session",
                Entries =
                [
                    new ClientLogEntryRequest
                    {
                        Timestamp = 1,
                        Level = ClientLogLevel.Info,
                        Event = "test_event",
                        Screen = "feed",
                    },
                ],
            },
            CancellationToken.None);

        var logCount = await Database
            .GetCollection<BsonDocument>("logs")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await Assert.That(logCount).IsEqualTo(0);
    }
}