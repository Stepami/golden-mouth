using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Auth;
using Reki.Api.Requests.Player;
using Reki.Domain.Player;
using Reki.Infrastructure.Auth;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class AuthControllerTests(ApiFactory factory)
{
    private const string ClientId = "reki-test-client";

    [After(Test)]
    public Task ClearGameDataAsync(CancellationToken cancellationToken) =>
        factory.Session.ClearGameDataAsync(cancellationToken);

    [Test]
    public async Task MiniApp_CompleteSessionLifecycle_KeepsOtherSessionAsync()
    {
        // Arrange
        var initData = SignInitData(factory.Session.TelegramBotToken, 21001);
        AuthTokenResponse first;
        AuthTokenResponse second;
        await using (var firstFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
        })
        {
            using var firstClient = firstFactory.CreateClient();
            var exchange = new ExternalTelegramAuthClient(firstClient);
            var missing = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() => ExchangeAsync(firstClient, exchange, initData))
                ?? throw new InvalidOperationException("Expected registration_required.");
            await Assert.That(missing.StatusCode).IsEqualTo(400);
            using var body = JsonDocument.Parse(missing.Response);
            await Assert.That(body.RootElement.GetProperty("error").GetString()).IsEqualTo("registration_required");
            var externalId = body.RootElement.GetProperty("external_user_id").GetString()
                ?? throw new InvalidOperationException("External ID is missing.");
            await Assert.That(externalId).IsEqualTo("telegram:21001");
            await Assert.That(await factory.Session.Database.GetCollection<BsonDocument>("players")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
            firstClient.DefaultRequestHeaders.Authorization = Basic(externalId);
            await exchange.CompleteOnboardingAsync(new OnboardingRequest { BirthGroup = BirthGroup.Teen, Address = Address.Nick }, CancellationToken.None);
            first = await ExchangeAsync(firstClient, exchange, initData);
            second = await ExchangeAsync(firstClient, exchange, initData);
        }

        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
        };
        using var client = productionFactory.CreateClient();
        var auth = new AuthClient(client);

        // Act
        using var playerHttp = productionFactory.CreateClient();
        var playerClient = new PlayerClient(playerHttp);
        var anonymousGame = await Assert.ThrowsAsync<PlayerApiException>(() => playerClient.GetCurrentPlayerAsync(CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a protected player response.");
        playerHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        var me = await playerClient.GetCurrentPlayerAsync(CancellationToken.None);
        var wrongRefreshRequest = new RefreshTokenRequest
        {
            ClientId = ClientId,
            GrantType = AuthConstants.Grants.MiniApp,
            RefreshToken = first.RefreshToken,
        };
        var wrongRefreshGrant = await Assert.ThrowsAsync<AuthApiException>(() => auth.RefreshAsync(
            wrongRefreshRequest, CancellationToken.None)) ?? throw new InvalidOperationException("Expected a rejected refresh grant.");
        var refreshed = await auth.RefreshAsync(RefreshRequest(first.RefreshToken), CancellationToken.None);
        var logoutRequest = new LogoutRequest
        {
            RefreshToken = refreshed.RefreshToken,
        };
        await auth.LogoutAsync(logoutRequest, CancellationToken.None);
        playerHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        var issuedAccess = await playerClient.GetCurrentPlayerAsync(CancellationToken.None);
        var revoked = await Assert.ThrowsAsync<AuthApiException>(() => auth.RefreshAsync(
            RefreshRequest(refreshed.RefreshToken), CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected refresh response.");
        var otherSession = await auth.RefreshAsync(RefreshRequest(second.RefreshToken), CancellationToken.None);

        // Assert
        await Assert.That(first.AccessToken).IsNotEmpty();
        await Assert.That(first.RefreshToken).IsNotEmpty();
        await Assert.That(first.TokenType).IsEqualTo("Bearer");
        await Assert.That(first.ExpiresIn).IsGreaterThan(0);
        await Assert.That(me.Energy).IsEqualTo(10);
        await Assert.That(anonymousGame.StatusCode).IsEqualTo(401);
        await Assert.That(wrongRefreshGrant.StatusCode).IsEqualTo(401);
        await Assert.That(refreshed.AccessToken).IsNotEmpty();
        await Assert.That(refreshed.RefreshToken).IsNotEqualTo(first.RefreshToken);
        await Assert.That(issuedAccess.Id).IsEqualTo(me.Id);
        await Assert.That(revoked.StatusCode).IsEqualTo(400);
        await Assert.That(otherSession.AccessToken).IsNotEmpty();
        playerHttp.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherSession.AccessToken);
        await Assert.That((await playerClient.GetCurrentPlayerAsync(CancellationToken.None)).Id).IsEqualTo(me.Id);
    }

    [Test]
    public async Task MiniApp_InvalidSignature_DoesNotIssueTokensAsync()
    {
        // Arrange
        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
        };
        using var client = productionFactory.CreateClient();
        var initData = SignInitData(factory.Session.TelegramBotToken, 21002);
        var tampered = initData[..^1] + (initData[^1] == '0' ? "1" : "0");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            AuthConstants.Schemes.MiniApp,
            tampered);

        // Act
        var rejected = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            new ExternalTelegramAuthClient(client).ExchangeMiniAppAsync(MiniAppRequest(), CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Mini App proof.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            AuthConstants.Schemes.MiniApp,
            initData);
        var wrongGrant = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            new ExternalTelegramAuthClient(client).ExchangeMiniAppAsync(
                new TokenExchangeRequest { ClientId = ClientId, GrantType = AuthConstants.Grants.TelegramLogin }, CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Mini App grant.");

        // Assert
        await Assert.That(rejected.StatusCode).IsGreaterThanOrEqualTo(400);
        await Assert.That(rejected.Response.Contains("external_user_id", StringComparison.Ordinal)).IsFalse();
        await Assert.That(wrongGrant.StatusCode).IsEqualTo(403);
    }

    [Test]
    public async Task MiniApp_ForwardedHttpsInProduction_IssuesTokensAsync()
    {
        // Arrange
        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
            UseProductionEnvironment = true,
        };
        using var client = productionFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        var exchange = new ExternalTelegramAuthClient(client);

        // Act
        var initData = SignInitData(factory.Session.TelegramBotToken, 21004);
        client.DefaultRequestHeaders.Authorization = Basic(ExternalUserId.Telegram(21004).Value);
        await exchange.CompleteOnboardingAsync(new OnboardingRequest { BirthGroup = BirthGroup.Teen, Address = Address.Nick }, CancellationToken.None);
        var token = await ExchangeAsync(client, exchange, initData);

        // Assert
        await Assert.That(token.AccessToken).IsNotEmpty();
        await Assert.That(token.RefreshToken).IsNotEmpty();
    }

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

    [Test]
    public async Task TelegramLogin_ValidIdToken_OpensExistingProfileAsync()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        using var metadata = WireMockServer.Start();
        var key = rsa.ExportParameters(false);
        metadata.Given(Request.Create().WithPath("/.well-known/openid-configuration").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"issuer":"{{AuthConstants.Issuers.Telegram}}","jwks_uri":"{{metadata.Url}}/.well-known/jwks.json"}"""));
        metadata.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"keys":[{"kty":"RSA","use":"sig","kid":"test-key","alg":"RS256","n":"{{Base64Url(key.Modulus ?? throw new InvalidOperationException("RSA modulus is missing."))}}","e":"{{Base64Url(key.Exponent ?? throw new InvalidOperationException("RSA exponent is missing."))}}"}]}"""));
        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
            TelegramOAuthUrl = metadata.Url + "/",
        };
        using var client = productionFactory.CreateClient();
        var exchange = new ExternalTelegramAuthClient(client);
        using var invalidKey = RSA.Create(2048);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SignTelegramIdToken(invalidKey, 21003));
        var rejectedProof = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            exchange.ExchangeTelegramLoginAsync(new TokenExchangeRequest { ClientId = ClientId, GrantType = AuthConstants.Grants.TelegramLogin }, CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Widget proof.");
        await Assert.That(rejectedProof.StatusCode).IsGreaterThanOrEqualTo(400);
        await Assert.That(rejectedProof.Response.Contains("external_user_id", StringComparison.Ordinal)).IsFalse();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SignTelegramIdToken(rsa, 21003));
        var rawTelegramGame = await Assert.ThrowsAsync<PlayerApiException>(() =>
            new PlayerClient(client).GetCurrentPlayerAsync(CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Telegram identity token on game API.");
        var widgetRequest = new TokenExchangeRequest
        {
            ClientId = ClientId,
            GrantType = AuthConstants.Grants.TelegramLogin,
        };
        var missing = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            exchange.ExchangeTelegramLoginAsync(widgetRequest, CancellationToken.None))
            ?? throw new InvalidOperationException("Expected registration_required.");
        await Assert.That(missing.StatusCode).IsEqualTo(400);
        using var body = JsonDocument.Parse(missing.Response);
        await Assert.That(body.RootElement.GetProperty("error").GetString()).IsEqualTo("registration_required");
        var externalId = body.RootElement.GetProperty("external_user_id").GetString()
                ?? throw new InvalidOperationException("External ID is missing.");
        await Assert.That(externalId).IsEqualTo("telegram:21003");
        await Assert.That(await factory.Session.Database.GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
        client.DefaultRequestHeaders.Authorization = Basic(externalId);
        await exchange.CompleteOnboardingAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Teen, Address = Address.Nick }, CancellationToken.None);
        var initData = SignInitData(factory.Session.TelegramBotToken, 21003);
        var mini = await ExchangeAsync(client, exchange, initData);
        using var miniClient = productionFactory.CreateClient();
        miniClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mini.AccessToken);
        var miniPlayer = await new PlayerClient(miniClient).GetCurrentPlayerAsync(CancellationToken.None);

        // Act
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SignTelegramIdToken(rsa, 21003));
        var widget = await exchange.ExchangeTelegramLoginAsync(widgetRequest, CancellationToken.None);
        var wrongGrant = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            exchange.ExchangeTelegramLoginAsync(
                new TokenExchangeRequest { ClientId = ClientId, GrantType = AuthConstants.Grants.MiniApp }, CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Telegram Login grant.");
        using var widgetClient = productionFactory.CreateClient();
        widgetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", widget.AccessToken);
        var current = await new PlayerClient(widgetClient).GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(widget.TokenType).IsEqualTo("Bearer");
        await Assert.That(rawTelegramGame.StatusCode).IsEqualTo(401);
        await Assert.That(wrongGrant.StatusCode).IsEqualTo(403);
        await Assert.That(widget.RefreshToken).IsNotEmpty();
        await Assert.That(current.Id).IsEqualTo(miniPlayer.Id);
    }

    private static AuthenticationHeaderValue Basic(string externalUserId) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(externalUserId)));

    private static async Task<AuthTokenResponse> ExchangeAsync(
        HttpClient client,
        ExternalTelegramAuthClient exchange,
        string initData)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.Schemes.MiniApp, initData);
        return await exchange.ExchangeMiniAppAsync(MiniAppRequest(), CancellationToken.None);
    }

    private static TokenExchangeRequest MiniAppRequest() => new()
    {
        ClientId = ClientId,
        GrantType = AuthConstants.Grants.MiniApp,
    };

    private static RefreshTokenRequest RefreshRequest(string refreshToken) => new()
    {
        ClientId = ClientId,
        GrantType = AuthConstants.Grants.Refresh,
        RefreshToken = refreshToken,
    };

    private static string SignInitData(string botToken, long userId)
    {
        var authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var user = $$"""{"id":{{userId}},"first_name":"Test"}""";
        var dataCheckString = $"auth_date={authDate}\nuser={user}";
        var secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(dataCheckString))).ToLowerInvariant();
        return $"auth_date={authDate}&user={Uri.EscapeDataString(user)}&hash={hash}";
    }

    private static string SignTelegramIdToken(RSA rsa, long userId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string header = """{"alg":"RS256","typ":"JWT","kid":"test-key"}""";
        var payload = $$"""{"iss":"{{AuthConstants.Issuers.Telegram}}","aud":"{{TestSessionFixture.TelegramBotId}}","iat":{{now}},"exp":{{now + 600}},"id":"{{userId}}"}""";
        var data = $"{Base64Url(Encoding.UTF8.GetBytes(header))}.{Base64Url(Encoding.UTF8.GetBytes(payload))}";
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{data}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
