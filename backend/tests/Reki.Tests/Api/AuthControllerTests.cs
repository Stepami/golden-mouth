using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
        var me = await Assert.ThrowsAsync<PlayerApiException>(() => playerClient.GetCurrentPlayerAsync(CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a missing profile response.");
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
        var issuedAccess = await Assert.ThrowsAsync<PlayerApiException>(() => playerClient.GetCurrentPlayerAsync(CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a missing profile response.");
        var revoked = await Assert.ThrowsAsync<AuthApiException>(() => auth.RefreshAsync(
            RefreshRequest(refreshed.RefreshToken), CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected refresh response.");
        var otherSession = await auth.RefreshAsync(RefreshRequest(second.RefreshToken), CancellationToken.None);

        // Assert
        await Assert.That(first.AccessToken).IsNotEmpty();
        await Assert.That(first.RefreshToken).IsNotEmpty();
        await Assert.That(first.TokenType).IsEqualTo("Bearer");
        await Assert.That(first.ExpiresIn).IsGreaterThan(0);
        await Assert.That(me.StatusCode).IsEqualTo(404);
        await Assert.That(anonymousGame.StatusCode).IsEqualTo(401);
        await Assert.That(wrongRefreshGrant.StatusCode).IsEqualTo(401);
        await Assert.That(refreshed.AccessToken).IsNotEmpty();
        await Assert.That(refreshed.RefreshToken).IsNotEqualTo(first.RefreshToken);
        await Assert.That(issuedAccess.StatusCode).IsEqualTo(404);
        await Assert.That(revoked.StatusCode).IsEqualTo(400);
        await Assert.That(otherSession.AccessToken).IsNotEmpty();
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            AuthConstants.Schemes.MiniApp,
            SignInitData(factory.Session.TelegramBotToken, 21002) + "tampered");

        // Act
        var rejected = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            new ExternalTelegramAuthClient(client).ExchangeMiniAppAsync(MiniAppRequest(), CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Mini App proof.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            AuthConstants.Schemes.MiniApp,
            SignInitData(factory.Session.TelegramBotToken, 21002));
        var wrongGrant = await Assert.ThrowsAsync<ExternalTelegramAuthApiException>(() =>
            new ExternalTelegramAuthClient(client).ExchangeMiniAppAsync(
                new TokenExchangeRequest { ClientId = ClientId, GrantType = AuthConstants.Grants.TelegramLogin }, CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Mini App grant.");

        // Assert
        await Assert.That(rejected.StatusCode).IsGreaterThanOrEqualTo(400);
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
        var token = await ExchangeAsync(client, exchange, SignInitData(factory.Session.TelegramBotToken, 21004));

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
        var mini = await ExchangeAsync(client, exchange, SignInitData(factory.Session.TelegramBotToken, 21003));
        using var miniClient = productionFactory.CreateClient();
        miniClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mini.AccessToken);
        var onboarding = new OnboardingRequest
        {
            BirthGroup = BirthGroup.Teen,
            Address = Address.Nick,
        };
        var player = await new PlayerClient(miniClient).CompleteOnboardingAsync(onboarding, CancellationToken.None);

        // Act
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SignTelegramIdToken(rsa, 21003));
        var rawTelegramGame = await Assert.ThrowsAsync<PlayerApiException>(() =>
            new PlayerClient(client).GetCurrentPlayerAsync(CancellationToken.None))
            ?? throw new InvalidOperationException("Expected a rejected Telegram identity token on game API.");
        var widgetRequest = new TokenExchangeRequest
        {
            ClientId = ClientId,
            GrantType = AuthConstants.Grants.TelegramLogin,
        };
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
        await Assert.That(current.Id).IsEqualTo(player.Id);
    }

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
