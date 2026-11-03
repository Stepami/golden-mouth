using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Reki.Api.Requests.Player;
using Reki.Domain.Player;
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
    private const string MiniAppGrant = "urn:reki:grant:telegram-mini-app";

    [Test]
    public async Task MiniApp_CompleteSessionLifecycle_KeepsOtherSessionAsync()
    {
        // Arrange
        var initData = SignInitData(factory.Session.TelegramBotToken, 21001);
        TokenPair first;
        TokenPair second;
        await using (var firstFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
        })
        {
            using var firstClient = firstFactory.CreateClient();
            first = await ExchangeAsync(firstClient, initData);
            second = await ExchangeAsync(firstClient, initData);
        }

        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
        };
        using var client = productionFactory.CreateClient();

        // Act
        using var me = new HttpRequestMessage(HttpMethod.Get, "/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        using var meResponse = await client.SendAsync(me);
        var refreshed = await RefreshAsync(client, first.RefreshToken);
        using var logout = await client.PostAsync("/auth/logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshed.RefreshToken,
        }));
        using var issuedAccess = new HttpRequestMessage(HttpMethod.Get, "/me");
        issuedAccess.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        using var issuedAccessResponse = await client.SendAsync(issuedAccess);
        using var revoked = await client.PostAsync("/auth/refresh", RefreshForm(refreshed.RefreshToken));
        var otherSession = await RefreshAsync(client, second.RefreshToken);

        // Assert
        await Assert.That(first.AccessToken).IsNotEmpty();
        await Assert.That(first.RefreshToken).IsNotEmpty();
        await Assert.That(first.TokenType).IsEqualTo("Bearer");
        await Assert.That(first.ExpiresIn).IsGreaterThan(0);
        await Assert.That(meResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(refreshed.AccessToken).IsNotEmpty();
        await Assert.That(refreshed.RefreshToken).IsNotEqualTo(first.RefreshToken);
        await Assert.That(logout.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(issuedAccessResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(revoked.IsSuccessStatusCode).IsFalse();
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/external/tg/miniapp")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["grant_type"] = MiniAppGrant,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "TMiniApp",
            SignInitData(factory.Session.TelegramBotToken, 21002) + "tampered");

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
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
                .WithBody("""{"issuer":"https://oauth.telegram.org","jwks_uri":"https://oauth.telegram.org/.well-known/jwks.json"}"""));
        metadata.Given(Request.Create().WithPath("/.well-known/jwks.json").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"keys":[{"kty":"RSA","use":"sig","kid":"test-key","alg":"RS256","n":"{{Base64Url(key.Modulus ?? throw new InvalidOperationException("RSA modulus is missing."))}}","e":"{{Base64Url(key.Exponent ?? throw new InvalidOperationException("RSA exponent is missing."))}}"}]}"""));
        await using var productionFactory = new ApiFactory
        {
            Session = factory.Session,
            UseProductionAuthentication = true,
            TelegramMetadataProxyUrl = metadata.Url + "/",
        };
        using var client = productionFactory.CreateClient();
        var mini = await ExchangeAsync(client, SignInitData(factory.Session.TelegramBotToken, 21003));
        using var miniClient = productionFactory.CreateClient();
        miniClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mini.AccessToken);
        var onboarding = new OnboardingRequest
        {
            BirthGroup = BirthGroup.Teen,
            Address = Address.Nick,
        };
        var player = await new PlayerClient(miniClient).CompleteOnboardingAsync(onboarding, CancellationToken.None);

        // Act
        using var widgetRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/external/tg/widget")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["grant_type"] = "urn:reki:grant:telegram-login",
            }),
        };
        widgetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SignTelegramIdToken(rsa, 21003));
        using var widgetResponse = await client.SendAsync(widgetRequest);
        await Assert.That(widgetResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var widget = await ReadPairAsync(widgetResponse);
        using var widgetClient = productionFactory.CreateClient();
        widgetClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", widget.AccessToken);
        var current = await new PlayerClient(widgetClient).GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(widget.TokenType).IsEqualTo("Bearer");
        await Assert.That(widget.RefreshToken).IsNotEmpty();
        await Assert.That(current.Id).IsEqualTo(player.Id);
    }

    private static async Task<TokenPair> ExchangeAsync(HttpClient client, string initData)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/external/tg/miniapp")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["grant_type"] = MiniAppGrant,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("TMiniApp", initData);
        using var response = await client.SendAsync(request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return await ReadPairAsync(response);
    }

    private static async Task<TokenPair> RefreshAsync(HttpClient client, string refreshToken)
    {
        using var response = await client.PostAsync("/auth/refresh", RefreshForm(refreshToken));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return await ReadPairAsync(response);
    }

    private static FormUrlEncodedContent RefreshForm(string refreshToken) => new(new Dictionary<string, string>
    {
        ["client_id"] = ClientId,
        ["grant_type"] = "refresh_token",
        ["refresh_token"] = refreshToken,
    });

    private static async Task<TokenPair> ReadPairAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        return new TokenPair(
            root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Access token is missing."),
            root.GetProperty("refresh_token").GetString() ?? throw new InvalidOperationException("Refresh token is missing."),
            root.GetProperty("token_type").GetString() ?? throw new InvalidOperationException("Token type is missing."),
            root.GetProperty("expires_in").GetInt32());
    }

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
        var payload = $$"""{"iss":"https://oauth.telegram.org","aud":"reki-telegram-test","iat":{{now}},"exp":{{now + 600}},"id":"{{userId}}"}""";
        var data = $"{Base64Url(Encoding.UTF8.GetBytes(header))}.{Base64Url(Encoding.UTF8.GetBytes(payload))}";
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{data}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record TokenPair(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);
}
