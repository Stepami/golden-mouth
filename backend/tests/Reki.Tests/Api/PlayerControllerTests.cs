using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Contracts.Player;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Domain.Player;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class PlayerControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task GetPlayer_WithExplicitNormalization_PreservesReadSemanticsAsync(bool normalizeDay)
    {
        // Arrange
        const long telegramUserId = 8414;
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var player = await store.GetPlayerAsync(TelegramUserId.From(telegramUserId), CancellationToken.None)
                     ?? throw new InvalidOperationException("Missing player.");
        var oldPeriod = player.PostsPeriodStartedAtUtc.AddDays(-1);
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with { PostsPeriodStartedAtUtc = oldPeriod, PostsUsed = 8 },
            cancellationToken: CancellationToken.None);
        using var scope = Factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Act
        var result = await sender.Send(new GetPlayerQuery(player.Id, normalizeDay), CancellationToken.None);

        // Assert
        var saved = await store.GetPlayerAsync(player.Id, CancellationToken.None)
                    ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(result.Id).IsEqualTo(player.Id);
        await Assert.That(result.PostsUsed).IsEqualTo(normalizeDay ? 0 : 8);
        await Assert.That(saved.PostsUsed).IsEqualTo(result.PostsUsed);
        await Assert.That(saved.PostsPeriodStartedAtUtc)
            .IsEqualTo(normalizeDay ? player.PostsPeriodStartedAtUtc : oldPeriod);
    }

    [Test]
    public async Task Me_WithoutTelegramAuthentication_ReturnsUnauthorizedAsync()
    {
        using var api = Factory.CreateApiClient();

        PlayerApiException? exception = null;
        try
        {
            await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        }
        catch (PlayerApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the unauthorized response.");
        }

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task Onboarding_ConcurrentDifferentUsers_KeepsIdentityIsolatedAsync()
    {
        // Arrange
        const long firstUserId = 9101;
        const long secondUserId = 9102;
        using var firstApi = Factory.CreateApiClient(firstUserId);
        using var secondApi = Factory.CreateApiClient(secondUserId);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var before = await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None);

        // Act
        var onboarded = await Task.WhenAll(
            CreatePlayerWithWorldAsync(firstApi, WorldId.Stream),
            CreatePlayerWithWorldAsync(secondApi, WorldId.Academy));
        var current = await Task.WhenAll(
            firstApi.Player.GetCurrentPlayerAsync(CancellationToken.None),
            secondApi.Player.GetCurrentPlayerAsync(CancellationToken.None));

        // Assert
        await Assert.That(await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None)).IsEqualTo(before + 2);
        await Assert.That(onboarded[0].Id).IsNotEqualTo(onboarded[1].Id);
        await Assert.That(current[0].Id).IsEqualTo(onboarded[0].Id);
        await Assert.That(current[1].Id).IsEqualTo(onboarded[1].Id);
        await Assert.That((await firstApi.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().BalanceKey).IsEqualTo("stream");
        await Assert.That((await secondApi.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().BalanceKey).IsEqualTo("academy");
        foreach ((var userId, var player) in new[]
                 {
                     (firstUserId, onboarded[0]),
                     (secondUserId, onboarded[1]),
                 })
        {
            var persisted = await Database.GetCollection<BsonDocument>("players")
                .Find(new BsonDocument("TelegramUserId", userId))
                .SingleAsync(CancellationToken.None);
            await Assert.That(persisted["_id"].AsString).IsEqualTo(player.Id);
        }
    }

    [Test]
    public async Task Onboarding_WithoutAuthentication_DoesNotCreatePlayerAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient();
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var before = await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None);

        // Act
        PlayerApiException? exception = null;
        try
        {
            await api.Player.CompleteOnboardingAsync(
                new OnboardingRequest
                {
                    BirthGroup = BirthGroup.Teen,
                    Address = Address.Nick,
                },
                CancellationToken.None);
        }
        catch (PlayerApiException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception?.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        var after = await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    public async Task OnboardPlayer_WithoutHttpContext_DoesNotCreatePlayerAsync()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new OnboardPlayerCommand(
            "14-17",
            "default",
            null,
            null);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var before = await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None);

        // Act
        InvalidOperationException? exception = null;
        try
        {
            await sender.Send(command, CancellationToken.None);
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        // Assert
        await Assert.That(exception?.Message).IsEqualTo("Failed to extract auth context from HTTP context");
        var after = await store.GetPlayersAsync(CancellationToken.None).CountAsync(CancellationToken.None);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    public async Task Onboarding_WithAgeBand_PersistsCoarseBandAsync()
    {
        const long userId = 10101;
        using var api = Factory.CreateApiClient(userId);
        await api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Adult,
                AgeBand = "45+",
                Address = Address.Nick,
            },
            CancellationToken.None);
        var profile = await GetStoredPlayerAsync(userId);
        await Assert.That(profile["AgeBand"].AsString).IsEqualTo("45+");
        await Assert.That(profile["BirthGroup"].AsString).IsEqualTo("18+");
    }

    [Test]
    public async Task Onboarding_WithEnumContracts_PersistsAgeGroupWithoutBirthYearAsync()
    {
        const long telegramUserId = 1001;
        using var api = Factory.CreateApiClient(telegramUserId);

        var response = await api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Teen,
                Address = Address.Nick,
            },
            CancellationToken.None);

        await Assert.That(response.PostsCap).IsEqualTo(10);
        await Assert.That(response.PostsLeft).IsEqualTo(10);
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).Id).IsEqualTo(response.Id);
        await Assert.That((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Count).IsEqualTo(0);

        var player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        await Assert.That(player["_id"].BsonType).IsEqualTo(BsonType.String);
        await Assert.That(player["TelegramUserId"].BsonType).IsEqualTo(BsonType.Int64);
        await Assert.That(player.Contains("World")).IsFalse();
        await Assert.That(player["Skills"].AsBsonDocument.Names)
            .IsEquivalentTo(["brave", "humor", "topic"]);
        await Assert.That(player["BirthGroup"].AsString).IsEqualTo("14-17");
        await Assert.That(player.Names.Any(name => name.Contains("year", StringComparison.OrdinalIgnoreCase)))
            .IsFalse();
    }

    [Test]
    public async Task Referral_NewPlayerFromCode_AppliesInviteAsync()
    {
        // Arrange
        using var inviterApi = Factory.CreateApiClient(8001);
        using var otherApi = Factory.CreateApiClient(8002);
        using var friendApi = Factory.CreateApiClient(8003);
        await CreatePlayerWithWorldAsync(inviterApi);
        await CreatePlayerWithWorldAsync(otherApi);
        var first = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var other = await otherApi.Player.GetReferralCodeAsync(CancellationToken.None);

        // Act
        await CreatePlayerWithWorldAsync(friendApi, referralCode: first.Code);
        var repeated = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await GetStoredPlayerAsync(8003);
        var inviter = await GetStoredPlayerAsync(8001);

        // Assert
        await Assert.That(first.Code).IsEqualTo(repeated.Code);
        await Assert.That(first.Code == other.Code).IsFalse();
        await Assert.That(friend["InvitedBy"].AsString).IsEqualTo(inviter["_id"].AsString);
        await Assert.That(friend["InvitedAtUtc"].IsBsonNull).IsFalse();
        await Assert.That(friend["DailyBonus"].AsInt32).IsEqualTo(0);
        await Assert.That(friend["FirstPostInviteBonus"].AsInt32).IsEqualTo(5);
        await Assert.That(friend["FirstPostInviteBonusUsed"].AsBoolean).IsFalse();
        await Assert.That(inviter["DailyBonus"].AsInt32).IsEqualTo(5);
    }

    [Test]
    public async Task Referral_InvalidSource_DoesNotCreateAttributionAsync()
    {
        // Arrange
        using var unknownApi = Factory.CreateApiClient(8004);
        using var malformedApi = Factory.CreateApiClient(8005);

        // Act
        await CreatePlayerWithWorldAsync(unknownApi, referralCode: $"ref_{Guid.NewGuid():N}");
        await CreatePlayerWithWorldAsync(malformedApi, referralCode: "invalid-code");
        await PublishAsync(8004, "первый пост без пригласителя");

        // Assert
        foreach (var userId in new long[] { 8004, 8005 })
        {
            var player = await GetStoredPlayerAsync(userId);
            await Assert.That(player["InvitedBy"].IsBsonNull).IsTrue();
            await Assert.That(player["DailyBonus"].AsInt32).IsEqualTo(0);
            await Assert.That(player["FirstPostInviteBonus"].AsInt32).IsEqualTo(0);
        }

        var noInviter = await unknownApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(noInviter.PostsCap).IsEqualTo(10);
        await Assert.That(noInviter.FirstPostInviteBonus).IsEqualTo(0);
    }

    [Test]
    public async Task Onboarding_ConcurrentInvitations_PreservesAttributionAsync()
    {
        // Arrange
        using var inviterApi = Factory.CreateApiClient(8007);
        using var friendApi = Factory.CreateApiClient(8008);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);

        // Act
        var results = await Task.WhenAll(
            OnboardOrConflictAsync(),
            OnboardOrConflictAsync());
        var friend = await GetStoredPlayerAsync(8008);
        var inviter = await GetStoredPlayerAsync(8007);

        // Assert
        await Assert.That(results.Any(result => result is not null)).IsTrue();
        foreach (var result in results.OfType<PlayerResponse>())
        {
            await Assert.That(result.Id).IsEqualTo(friend["_id"].AsString);
        }

        await Assert.That(friend["InvitedBy"].AsString).IsEqualTo(referral.Code[4..]);
        await Assert.That(inviter["DailyBonus"].AsInt32).IsEqualTo(5);

        async Task<PlayerResponse?> OnboardOrConflictAsync()
        {
            try
            {
                return await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
            }
            catch (PlayerApiException exception) when (exception.StatusCode == StatusCodes.Status409Conflict)
            {
                return null;
            }
        }
    }

    [Test]
    public async Task Referral_InviterWithUsedPosts_OnboardingAddsFiveRemainingPostsAsync()
    {
        // Arrange
        const long inviterUserId = 8017;
        using var inviterApi = Factory.CreateApiClient(inviterUserId);
        using var friendApi = Factory.CreateApiClient(8018);
        await CreatePlayerWithWorldAsync(inviterApi);
        await PublishAsync(inviterUserId, "пост пригласителя");
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);

        // Act
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var inviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(friend.PostsCap).IsEqualTo(10);
        await Assert.That(inviter.PostsCap).IsEqualTo(15);
        await Assert.That(inviter.PostsLeft).IsEqualTo(14);
    }

    [Test]
    public async Task Referral_InvitedPlayerPublishesFirstPost_AppliesStoredBonusOnceAsync()
    {
        // Arrange
        const long inviterUserId = 8010;
        const long friendUserId = 8011;
        using var inviterApi = Factory.CreateApiClient(inviterUserId);
        using var friendApi = Factory.CreateApiClient(friendUserId);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var onboarded = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var inviterAfterOnboarding = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Act
        await PublishAsync(friendUserId, "первый пост друга");
        var inviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var friend = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await PublishAsync(friendUserId, "второй пост друга");
        var storedInviter = await GetStoredPlayerAsync(inviterUserId);
        var storedFriend = await GetStoredPlayerAsync(friendUserId);

        // Assert
        await Assert.That(onboarded.PostsCap).IsEqualTo(10);
        await Assert.That(onboarded.FirstPostInviteBonus).IsEqualTo(5);
        await Assert.That(inviterAfterOnboarding.PostsCap).IsEqualTo(15);
        await Assert.That(inviter.PostsCap).IsEqualTo(15);
        await Assert.That(inviter.PostsLeft).IsEqualTo(15);
        await Assert.That(friend.PostsCap).IsEqualTo(15);
        await Assert.That(friend.PostsLeft).IsEqualTo(14);
        await Assert.That(friend.FirstPostInviteBonus).IsEqualTo(0);
        await Assert.That(storedInviter["DailyBonus"].AsInt32).IsEqualTo(5);
        await Assert.That(storedFriend["DailyBonus"].AsInt32).IsEqualTo(5);
        await Assert.That(storedFriend["FirstPostInviteBonusUsed"].AsBoolean).IsTrue();
    }

    [Test]
    public async Task Referral_FourFriendsOnboard_LimitsInviterToThreeBonusesAsync()
    {
        // Arrange
        const long inviterUserId = 8012;
        using var inviterApi = Factory.CreateApiClient(inviterUserId);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);

        // Act
        for (long friendUserId = 8013; friendUserId <= 8016; friendUserId++)
        {
            using var friendApi = Factory.CreateApiClient(friendUserId);
            await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        }

        await PublishAsync(8016, "первый пост четвёртого друга");
        var inviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var fourthFriend = await GetStoredPlayerAsync(8016);
        var storedInviter = await GetStoredPlayerAsync(inviterUserId);

        // Assert
        await Assert.That(inviter.PostsCap).IsEqualTo(25);
        await Assert.That(storedInviter["InviteBonusesEarnedInPeriod"].AsInt32).IsEqualTo(3);
        await Assert.That(fourthFriend["DailyBonus"].AsInt32).IsEqualTo(5);
    }

    [Test]
    public async Task Settings_WhenPlusIsEnabled_UsesPlusDailyLimitAsync()
    {
        using var api = Factory.CreateApiClient(1005);
        await CreatePlayerWithWorldAsync(api);

        await api.Player.UpdateSettingsAsync(
            new UpdateSettingsRequest { Plus = true },
            CancellationToken.None);
        var response = await api.Player.GetCurrentPlayerAsync(
            CancellationToken.None);

        await Assert.That(response.PostsCap).IsEqualTo(20);
        await Assert.That(response.PostsLeft).IsEqualTo(20);
    }

    [Test]
    public async Task DeleteAccount_RemovesPlayerDataFromMongoAsync()
    {
        const long telegramUserId = 1004;
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);

        var players = Database.GetCollection<BsonDocument>("players");
        var player = await players
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        var playerId = player["_id"].AsString;

        await PublishAsync(telegramUserId, "hello");
        await api.Telemetry.SubmitAnalyticsAsync(
            new AnalyticsBatchRequest
            {
                Session = "test-session",
                Platform = "test",
                Experiments = new Dictionary<string, string>(),
                Events =
                [
                    new AnalyticsEventRequest
                    {
                        Name = "screen_view",
                        Timestamp = 1,
                        Screen = "feed",
                    },
                ],
            },
            CancellationToken.None);
        await Database.GetCollection<BsonDocument>("invites").InsertOneAsync(
            new BsonDocument("PlayerId", playerId));

        await api.Player.DeleteCurrentPlayerAsync(CancellationToken.None);

        await AssertCollectionHasNoPlayerAsync(
            "players",
            new BsonDocument("TelegramUserId", telegramUserId));
        foreach (var collection in new[]
                 {
                     "posts",
                     "publications",
                     "threads",
                     "stars_memory",
                     "invites",
                     "challenges",
                     "events",
                     "analytics",
                     "worlds",
                 })
        {
            await AssertCollectionHasNoPlayerAsync(collection, new BsonDocument(collection == "worlds" ? "Player.PlayerId" : "PlayerId", playerId));
        }
    }

    [Test]
    public async Task Settings_PlayerPreferences_PersistAsync()
    {
        using var api = Factory.CreateApiClient(8400);
        await api.Player.CompleteOnboardingAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Teen, Address = Address.Nick },
            CancellationToken.None);
        foreach (var address in new[] { Address.She, Address.He, Address.Nick })
        {
            await api.Player.UpdateSettingsAsync(
                new UpdateSettingsRequest
                {
                    Address = address,
                    Notifications = "important",
                    Quiet = "21:30-08:15",
                },
                CancellationToken.None);
            var saved = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
            await Assert.That(saved.Address).IsEqualTo(address);
            await Assert.That(saved.Settings.Notifications).IsEqualTo("important");
            await Assert.That(saved.Settings.Quiet).IsEqualTo("21:30-08:15");
        }

        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Address = Address.She }, CancellationToken.None);
        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        await api.Social.SendDirectMessageAsync("sonya", await GetWorldIdAsync(api), new DirectMessageRequest { Text = "Привет" }, CancellationToken.None);
        using var profile = ParsePlayerLayer(GetCapturedChatMessages()[1]);
        await Assert.That(profile.RootElement.GetProperty("address").GetString()).IsEqualTo("she");
    }

    [Test]
    public async Task Settings_InvalidAddress_ReturnsBadRequestAsync()
    {
        using var api = Factory.CreateApiClient(8401);
        await CreatePlayerWithWorldAsync(api);
        using var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "8401");
        using var content = new StringContent("{\"address\":\"wizard\"}", Encoding.UTF8, "application/json");
        using var result = await http.PatchAsync(await WorldPathAsync(http, "/settings"), content, CancellationToken.None);
        await Assert.That(result.StatusCode).IsEqualTo(System.Net.HttpStatusCode.BadRequest);
        var saved = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(saved.Address).IsEqualTo(Address.Nick);
    }

    [Test]
    public async Task PlusInterest_BonusOnlyThenNotify_DoesNotRepeatBonusAsync()
    {
        using var api = Factory.CreateApiClient(8402);
        await CreatePlayerWithWorldAsync(api);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        using var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "8402");
        foreach (var interest in new[] { false, true, true })
        {
            using var body = new StringContent(interest ? "{\"interest\":true}" : "{\"interest\":false}", Encoding.UTF8, "application/json");
            using var response = await http.PostAsync("/plus-interest", body, CancellationToken.None);
            await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));
            await Assert.That(payload.RootElement.GetProperty("bonus").GetInt32()).IsEqualTo(interest ? 0 : 5);
            var saved = await store.GetPlayerAsync(TelegramUserId.From(8402), CancellationToken.None)
                ?? throw new InvalidOperationException("Missing player.");
            await Assert.That(saved.PlusNotifyRequested).IsEqualTo(interest);
            var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
            await Assert.That(current.PostsLeft).IsEqualTo(15);
        }

        using var legacyApi = Factory.CreateApiClient(8403);
        await CreatePlayerWithWorldAsync(legacyApi);
        http.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeaderName);
        http.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "8403");
        using var legacy = await http.PostAsync("/plus-interest", null, CancellationToken.None);
        await Assert.That(legacy.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        var legacyPlayer = await store.GetPlayerAsync(TelegramUserId.From(8403), CancellationToken.None)
            ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(legacyPlayer.PlusNotifyRequested).IsTrue();
    }

    [Test]
    public async Task PlusInterest_ConcurrentClaims_GrantExactlyOneBonusAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8404);
        await CreatePlayerWithWorldAsync(api);
        using var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "8404");

        // Act
        var calls = Enumerable.Range(0, 2).Select(_ =>
            http.PostAsync("/plus-interest", null, CancellationToken.None)).ToArray();
        var results = await Task.WhenAll(calls);

        // Assert
        var bonuses = 0;
        foreach (var result in results)
        {
            using (result)
            {
                await Assert.That(result.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);

                using var data =
                    JsonDocument.Parse(await result.Content.ReadAsStringAsync(CancellationToken.None));
                bonuses += data.RootElement.GetProperty("bonus").GetInt32();
            }
        }

        await Assert.That(bonuses).IsEqualTo(5);
        var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(current.PostsLeft).IsEqualTo(15);
    }

    [Test]
    public async Task Settings_PartialUpdates_PreserveOtherFieldsAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8410);
        await CreatePlayerWithWorldAsync(api);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var userId = TelegramUserId.From(8410);
        var player = await store.GetPlayerAsync(userId, CancellationToken.None)
                               ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                DailyBonus = 7,
                PostsUsed = 2,
                Settings = player.Settings with { Plus = true },
            },
            cancellationToken: CancellationToken.None);

        var world = (await store.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await store.TryReplaceWorldAsync(
            world with
        {
            Version = world.Version + 1,
            Player = world.Player with
            {
                Subscribers = 1234,
                Voice = "Коротко и тепло",
            },
        },
            world.Version,
            CancellationToken.None);

        // Act
        await Task.WhenAll(
            api.Worlds.UpdateWorldAsync(await GetWorldIdAsync(api), new Contracts.Worlds.UpdateWorldRequest { Drama = Drama.Off }, CancellationToken.None),
            api.Player.UpdateSettingsAsync(
                new UpdateSettingsRequest { Address = Address.She },
                CancellationToken.None));
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = false }, CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest(), CancellationToken.None);

        // Assert
        var saved = await store.GetPlayerAsync(userId, CancellationToken.None)
                              ?? throw new InvalidOperationException("Missing player.");
        var savedWorld = (await store.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Drama).IsEqualTo(Domain.Worlds.WorldDrama.Off);
        await Assert.That(saved.Address).IsEqualTo("she");
        await Assert.That(saved.Settings.Plus).IsFalse();
        await Assert.That(savedWorld.Player.Voice).IsEqualTo("Коротко и тепло");
        await Assert.That(saved.Settings.Notifications).IsEqualTo(player.Settings.Notifications);
        await Assert.That(saved.Settings.Quiet).IsEqualTo(player.Settings.Quiet);
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
        await Assert.That(saved.DailyBonus).IsEqualTo(7);
        await Assert.That(saved.PostsUsed).IsEqualTo(2);
    }

    [Test]
    public async Task Settings_MissingPlayer_ReturnsNotFoundAsync()
    {
        // Arrange
        using var http = CreateAuthenticatedHttpClient(8411);
        using var body = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await http.PatchAsync(await WorldPathAsync(http, "/settings"), body, CancellationToken.None);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        await Assert.That(await store.GetPlayerAsync(TelegramUserId.From(8411), CancellationToken.None)).IsNull();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task PlusInterest_NewPeriod_PreservesBonusRulesAsync(bool alreadyClaimed)
    {
        // Arrange
        long userId = alreadyClaimed ? 8413 : 8412;
        using var api = Factory.CreateApiClient(userId);
        await CreatePlayerWithWorldAsync(api);
        var store = Factory.Services.GetRequiredService<IGameStore>();
        var player = await store.GetPlayerAsync(TelegramUserId.From(userId), CancellationToken.None)
                               ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                PostsPeriodStartedAtUtc = player.PostsPeriodStartedAtUtc.AddDays(-1),
                PostsUsed = 8,
                DailyBonus = 7,
                InviteBonusesEarnedInPeriod = 2,
                PlusInterestClaimed = alreadyClaimed,
                PlusNotifyRequested = true,
            },
            cancellationToken: CancellationToken.None);
        var world = (await store.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await store.TryReplaceWorldAsync(
            world with
            {
                Stars = world.Stars.Select(star => star with { RelationGainedInPeriod = 3 }).ToArray(),
                Version = world.Version + 1,
                Player = world.Player with
                {
                    Subscribers = 1234,
                    PeriodStartedAtUtc = player.PostsPeriodStartedAtUtc.AddDays(-1),
                    Wants = world.Player.Wants.Select(want => want with { Done = true }).ToArray(),
                },
            },
            world.Version,
            CancellationToken.None);
        using var http = CreateAuthenticatedHttpClient(userId);

        // Act
        using var firstBody = new StringContent("{\"interest\":false}", Encoding.UTF8, "application/json");
        using var secondBody = new StringContent("{\"interest\":false}", Encoding.UTF8, "application/json");
        Task<HttpResponseMessage>[] calls =
        [
            http.PostAsync("/plus-interest", firstBody, CancellationToken.None),
            http.PostAsync("/plus-interest", secondBody, CancellationToken.None),
        ];
        var results = await Task.WhenAll(calls);

        // Assert
        var bonuses = 0;
        foreach (var result in results)
        {
            using (result)
            {
                await Assert.That(result.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
                using var data =
                    JsonDocument.Parse(await result.Content.ReadAsStringAsync(CancellationToken.None));
                bonuses += data.RootElement.GetProperty("bonus").GetInt32();
            }
        }

        await Assert.That(bonuses).IsEqualTo(alreadyClaimed ? 0 : 5);
        var saved = await store.GetPlayerAsync(TelegramUserId.From(userId), CancellationToken.None)
                              ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(saved.PlusInterestClaimed).IsTrue();
        await Assert.That(saved.PlusNotifyRequested).IsTrue();
        await Assert.That(saved.DailyBonus).IsEqualTo(alreadyClaimed ? 0 : 5);
        await Assert.That(saved.PostsUsed).IsEqualTo(0);
        await Assert.That(saved.InviteBonusesEarnedInPeriod).IsEqualTo(0);
        await Assert.That(saved.PostsPeriodStartedAtUtc).IsEqualTo(player.PostsPeriodStartedAtUtc);
        _ = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single(item => item.Id == world.Id.Value);
        var savedWorld = (await store.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Wants.All(want => !want.Done)).IsTrue();
        await Assert.That(savedWorld.Stars.All(relation => relation.RelationGainedInPeriod == 0 &&
                                                              relation.RelationPeriodStartedAtUtc ==
                                                              player.PostsPeriodStartedAtUtc)).IsTrue();
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
    }
}