namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class AuthControllerTests(ApiFactory factory)
{
    [After(Test)]
    public Task ClearGameDataAsync(CancellationToken cancellationToken) =>
        factory.Session.ClearGameDataAsync(cancellationToken);

    [Test]
    public async Task TelegramClientId_Anonymous_ReturnsSdkBotIdAsync()
    {
        // Arrange
        using var client = factory.CreateClient();

        // Act
        var botId = await new ExternalTelegramAuthClient(client).GetTelegramClientIdAsync(CancellationToken.None);
        using var openApi = await client.GetAsync("/openapi/v1.json");

        // Assert
        await Assert.That(botId).IsEqualTo(TestSessionFixture.TelegramBotId);
        await Assert.That(openApi.IsSuccessStatusCode).IsTrue();
        await Assert.That(factory.Services.GetFakeLogCollector().GetSnapshot()
            .Any(log => log.Message.Contains(factory.Session.TelegramBotToken, StringComparison.Ordinal))).IsFalse();
    }
}
