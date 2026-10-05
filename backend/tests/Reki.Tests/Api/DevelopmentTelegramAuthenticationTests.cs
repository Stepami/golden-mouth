using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Serialization;
using Reki.Contracts.Player;

namespace Reki.Tests.Api;

[NotInParallel]
[ClassDataSource<DevelopmentTelegramAuthenticationFactory>(Shared = SharedType.PerTestSession)]
public sealed class DevelopmentTelegramAuthenticationTests(
    DevelopmentTelegramAuthenticationFactory factory)
{
    [Test]
    public async Task Onboarding_DevelopmentInitData_AuthenticatesConfiguredTelegramUserAsync()
    {
        // Arrange
        const long telegramUserId = 8_001;
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = CreateOnboardingRequest(
            CreateDevelopmentInitData(telegramUserId));

        // Act
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        BsonDocument player = await factory.Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        await Assert.That(player["TelegramUserId"].AsInt64).IsEqualTo(telegramUserId);
    }

    [Test]
    public async Task Onboarding_MalformedDevelopmentInitData_ReturnsUnauthorizedAsync()
    {
        // Arrange
        const long telegramUserId = 8_002;
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = CreateOnboardingRequest(
            $"auth_date=1791201600&hash=development&user=%7B%22id%22%3A{telegramUserId}%7D");

        // Act
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        long players = await factory.Database
            .GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(new BsonDocument("TelegramUserId", telegramUserId));
        await Assert.That(players).IsEqualTo(0);
    }

    [Test]
    public async Task Onboarding_DevelopmentInitDataOutsideDevelopment_ReturnsUnauthorizedAsync()
    {
        // Arrange
        const long telegramUserId = 8_003;
        using HttpClient client = factory.CreateProductionClient();
        using HttpRequestMessage request = CreateOnboardingRequest(
            CreateDevelopmentInitData(telegramUserId));

        // Act
        using HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        long players = await factory.Database
            .GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(new BsonDocument("TelegramUserId", telegramUserId));
        await Assert.That(players).IsEqualTo(0);
    }

    private static HttpRequestMessage CreateOnboardingRequest(string initData)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/onboarding")
        {
            Content = JsonContent.Create(
                new OnboardingRequest
                {
                    BirthGroup = BirthGroup.Teen,
                    World = WorldId.Stage,
                    Role = "creator",
                    Avatar = "pick0",
                    Name = "Development Player",
                    Handle = "development-player",
                    Address = Address.Nick,
                    Star = "mira",
                },
                ApiJsonSerializerContext.Default.OnboardingRequest),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("TMiniApp", initData);
        return request;
    }

    private static string CreateDevelopmentInitData(long telegramUserId)
    {
        string user = $$"""{"id":{{telegramUserId}},"first_name":"Development player"}""";
        return string.Join(
            '&',
            $"user={Uri.EscapeDataString(user)}",
            $"auth_date={1791201600L.ToString(CultureInfo.InvariantCulture)}",
            "hash=development");
    }
}
