using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Reki.Contracts.Player;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class CurrentPlayerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Me_WithoutProfile_ReturnsCompatibleNotFoundWithoutExceptionTelemetryAsync()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var provider = Substitute.For<ILoggerProvider>();
        provider.CreateLogger(Arg.Any<string>()).Returns(logger);
        await using var observedFactory = new ApiFactory { Session = Factory.Session, RequestLogProvider = provider };
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "18301");
        logger.ClearReceivedCalls();
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
#pragma warning disable AV1562 // ActivityListener.Sample requires a ref parameter in its delegate signature.
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
#pragma warning restore AV1562
            ActivityStopped = activities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var response = await client.GetAsync("/me", CancellationToken.None);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var problem = body.RootElement;
        await Assert.That(problem.GetProperty("type").GetString()).IsEqualTo("urn:reki:problem:resource-not-found");
        await Assert.That(problem.GetProperty("title").GetString()).IsEqualTo("Resource not found");
        await Assert.That(problem.GetProperty("detail").GetString()).IsEqualTo("The requested resource was not found.");
        await Assert.That(problem.GetProperty("status").GetInt32()).IsEqualTo(404);
        await Assert.That(problem.GetProperty("instance").GetString()).IsEqualTo("/me");
        await Assert.That(problem.GetProperty("failure").ValueKind).IsEqualTo(JsonValueKind.Null);
        await Assert.That(await Database.GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(new BsonDocument("TelegramUserId", 18301L), cancellationToken: CancellationToken.None))
            .IsEqualTo(0L);
        var logs = logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log")
            .Select(call => call.GetArguments()).ToArray();
        await Assert.That(logs.Any(log => log[0] is LogLevel.Warning or LogLevel.Error or LogLevel.Critical)).IsFalse();
        await Assert.That(logs.Any(log => log[3] is Exception)).IsFalse();
        await Assert.That(logs.Any(log => log[2] is IEnumerable<KeyValuePair<string, object?>> properties &&
            properties.Any(property => property.Key == "StatusCode" && Equals(property.Value, 404)))).IsTrue();
        await Assert.That(activities.Any(activity => activity.OperationName.Contains("HttpRequestIn", StringComparison.Ordinal)))
            .IsTrue();
        await Assert.That(activities.SelectMany(activity => activity.Events)
            .Any(activityEvent => activityEvent.Name.Contains("exception", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task Me_AfterOnboarding_ReadsAuthenticatedProfileWithoutChangingStoredDataAsync()
    {
        // Arrange
        using var firstApi = Factory.CreateApiClient(18302);
        using var secondApi = Factory.CreateApiClient(18303);
        var request = new OnboardingRequest { BirthGroup = BirthGroup.Teen, Address = Address.Nick };
        var first = await firstApi.Player.CompleteOnboardingAsync(request, CancellationToken.None);
        var second = await secondApi.Player.CompleteOnboardingAsync(request, CancellationToken.None);
        var players = Database.GetCollection<BsonDocument>("players");
        var before = await players.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(CancellationToken.None);

        // Act
        var currentFirst = await firstApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var currentSecond = await secondApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var returningFirst = await firstApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(currentFirst.Id).IsEqualTo(first.Id);
        await Assert.That(currentSecond.Id).IsEqualTo(second.Id);
        await Assert.That(currentFirst.Id).IsNotEqualTo(currentSecond.Id);
        await Assert.That(returningFirst.Id).IsEqualTo(first.Id);
        await Assert.That(currentFirst.Energy).IsEqualTo(first.Energy);
        await Assert.That(currentSecond.Energy).IsEqualTo(second.Energy);
        var after = await players.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(CancellationToken.None);
        await Assert.That(after.OrderBy(player => player["_id"].AsString).Select(player => player.ToJson()).ToArray())
            .IsEquivalentTo(before.OrderBy(player => player["_id"].AsString).Select(player => player.ToJson()).ToArray());
    }

    [Test]
    public async Task Me_CorruptStoredProfile_ReturnsServerErrorWithDiagnosticsAsync()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var provider = Substitute.For<ILoggerProvider>();
        provider.CreateLogger(Arg.Any<string>()).Returns(logger);
        await using var observedFactory = new ApiFactory { Session = Factory.Session, RequestLogProvider = provider };
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "18304");
        await Database.GetCollection<BsonDocument>("players").InsertOneAsync(
            new BsonDocument { { "_id", "corrupt-profile" }, { "TelegramUserId", 18304L }, { "Energy", "invalid-integer" } },
            cancellationToken: CancellationToken.None);
        logger.ClearReceivedCalls();

        // Act
        using var response = await client.GetAsync("/me", CancellationToken.None);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(body.RootElement.GetProperty("type").GetString()).IsEqualTo("urn:reki:problem:unexpected-error");
        await Assert.That(logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Log")
            .Any(call => call.GetArguments()[0] is LogLevel.Error && call.GetArguments()[3] is Exception)).IsTrue();
    }
}
