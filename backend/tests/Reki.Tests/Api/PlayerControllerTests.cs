using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Telemetry;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class PlayerControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Me_WithoutProfile_ReturnsCompatibleNotFoundWithoutExceptionTelemetryAsync()
    {
        // Arrange
        await using var observedFactory = new ApiFactory { Session = Factory.Session };
        using var client = observedFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeaderName, "18301");
        var collector = observedFactory.Services.GetFakeLogCollector();
        collector.Clear();
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        var response = await Assert.ThrowsAsync<PlayerApiException>(() => new PlayerClient(client) { ReadResponseAsString = true }.GetCurrentPlayerAsync(CancellationToken.None));
        await Assert.That(response).IsNotNull();
        using var body = JsonDocument.Parse(response.Response);

        // Assert
        await Assert.That((HttpStatusCode)response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
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
        var logs = collector.GetSnapshot();
        await Assert.That(logs.Any(log => log.Level is LogLevel.Warning or LogLevel.Error or LogLevel.Critical)).IsFalse();
        await Assert.That(logs.Any(log => log.Exception is not null)).IsFalse();
        await Assert.That(logs.Any(log => log.GetStructuredStateValue("StatusCode") == "404")).IsTrue();
        await Assert.That(activities.Any(activity => activity.OperationName.Contains("HttpRequestIn", StringComparison.Ordinal)))
            .IsTrue();
        await Assert.That(activities.SelectMany(activity => activity.Events)
            .Any(activityEvent => activityEvent.Name.Contains("exception", StringComparison.OrdinalIgnoreCase))).IsFalse();
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
        var players = Database.GetCollection<BsonDocument>("players");
        var before = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);

        // Act
        var onboarded = await Task.WhenAll(
            CreatePlayerWithWorldAsync(firstApi, WorldId.Stream),
            CreatePlayerWithWorldAsync(secondApi, WorldId.Academy));
        var current = await Task.WhenAll(
            firstApi.Player.GetCurrentPlayerAsync(CancellationToken.None),
            secondApi.Player.GetCurrentPlayerAsync(CancellationToken.None));

        // Assert
        await Assert.That(await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None))
            .IsEqualTo(before + 2);
        await Assert.That(onboarded[0].Id).IsNotEqualTo(onboarded[1].Id);
        await Assert.That(current[0].Id).IsEqualTo(onboarded[0].Id);
        await Assert.That(current[1].Id).IsEqualTo(onboarded[1].Id);
        await Assert.That((await firstApi.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().BalanceKey)
            .IsEqualTo("stream");
        await Assert.That((await secondApi.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().BalanceKey)
            .IsEqualTo("academy");
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
        var players = Database.GetCollection<BsonDocument>("players");
        var before = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);

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
        var after = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
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

        await Assert.That(response.Energy).IsEqualTo(10);
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
        var invite = await GetStoredInviteAsync(friend["_id"].AsString);
        await Assert.That(invite["InviterId"]).IsEqualTo(inviter["_id"]);
        await Assert.That(friend.Contains("InvitedBy")).IsFalse();
        await Assert.That(friend["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(friend.Contains("FirstPostInviteBonus")).IsFalse();
        await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsFalse();
        await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsFalse();
        await Assert.That(invite["InviteeBonusUsedAtUtc"].IsBsonNull).IsTrue();
        await Assert.That(invite["InviterBonusUsedAtUtc"].IsBsonNull).IsTrue();
        await Assert.That(inviter["Energy"].AsInt32).IsEqualTo(10);
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
            await Assert.That(player.Contains("InvitedBy")).IsFalse();
            await Assert.That(await Database.GetCollection<BsonDocument>("invites").CountDocumentsAsync(
                new BsonDocument("InviteeId", player["_id"]))).IsEqualTo(0L);
            await Assert.That(player["Energy"].AsInt32).IsEqualTo(userId == 8004 ? 9 : 10);
            await Assert.That(player.Contains("FirstPostInviteBonus")).IsFalse();
        }

        var noInviter = await unknownApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(noInviter.Energy).IsEqualTo(9);
        await Assert.That((await unknownApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
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

        var invite = await GetStoredInviteAsync(friend["_id"].AsString);
        await Assert.That(invite["InviterId"].AsString).IsEqualTo(referral.Code[4..]);
        await Assert.That(inviter["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(await Database.GetCollection<BsonDocument>("invites").CountDocumentsAsync(
            new BsonDocument("InviteeId", friend["_id"]))).IsEqualTo(1L);
        await Assert.That(await Database.GetCollection<BsonDocument>("invites").CountDocumentsAsync(
            new BsonDocument("InviterId", inviter["_id"]))).IsEqualTo(1L);

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
        var pendingInvite = await friendApi.Player.GetInvitationAsync(CancellationToken.None);
        var inviterAfterOnboarding = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Act
        await PublishAsync(friendUserId, "первый пост друга");
        var inviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var friend = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await PublishAsync(friendUserId, "второй пост друга");
        var storedInviter = await GetStoredPlayerAsync(inviterUserId);
        var storedFriend = await GetStoredPlayerAsync(friendUserId);

        // Assert
        await Assert.That(onboarded.Energy).IsEqualTo(10);
        await Assert.That(pendingInvite.Bonus).IsEqualTo(5);
        await Assert.That(inviterAfterOnboarding.Energy).IsEqualTo(10);
        await Assert.That(inviterAfterOnboarding.HitOwnWorldMilestone).IsFalse();
        await Assert.That(inviter.HitOwnWorldMilestone).IsTrue();
        await Assert.That(inviter.Energy).IsEqualTo(15);
        await Assert.That(friend.Energy).IsEqualTo(14);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
        await Assert.That(storedInviter["Energy"].AsInt32).IsEqualTo(15);
        await Assert.That(storedFriend["Energy"].AsInt32).IsEqualTo(13);
        var invite = await GetStoredInviteAsync(storedFriend["_id"].AsString);
        await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsTrue();
        await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsTrue();
        await Assert.That(invite["InviterBonusUsedAtUtc"]).IsEqualTo(invite["InviteeBonusUsedAtUtc"]);
    }

    [Test]
    public async Task Referral_FourFriendsPublish_LimitsInviterToThreeBonusesAsync()
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
            await PublishAsync(friendUserId, "первый пост друга");
        }

        var inviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var fourthFriend = await GetStoredPlayerAsync(8016);
        var storedInviter = await GetStoredPlayerAsync(inviterUserId);

        // Assert
        await Assert.That(inviter.Energy).IsEqualTo(25);
        await Assert.That(storedInviter.Contains("InviteBonusesEarnedInPeriod")).IsFalse();
        await Assert.That(storedInviter["Energy"].AsInt32).IsEqualTo(25);
        await Assert.That(fourthFriend["Energy"].AsInt32).IsEqualTo(14);
        using var fourthApi = Factory.CreateApiClient(8016);
        await Assert.That((await fourthApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(14);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Referral_ExistingEnergy_AddsOnlyForUnusedInviteAsync(bool alreadyUsed)
    {
        using var inviterApi = Factory.CreateApiClient(8560);
        using var friendApi = Factory.CreateApiClient(8561);
        var inviter = await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var players = Database.GetCollection<BsonDocument>("players");
        await players.UpdateManyAsync(
            Builders<BsonDocument>.Filter.In("_id", new[] { inviter.Id, friend.Id }),
            Builders<BsonDocument>.Update.Set("Energy", 17));
        if (alreadyUsed)
        {
            var now = Factory.Session.Clock.GetUtcNow();
            await Database.GetCollection<Invitation>("invites").UpdateOneAsync(
                item => item.InviteeId == PlayerId.From(friend.Id),
                Builders<Invitation>.Update
                    .Set(item => item.InviteeBonusUsed, true)
                    .Set(item => item.InviterBonusUsed, true)
                    .Set(item => item.InviteeBonusUsedAtUtc, now)
                    .Set(item => item.InviterBonusUsedAtUtc, now));
        }

        await PublishAsync(8561, "пост с существующим бонусом");

        var expectedEnergy = alreadyUsed ? 17 : 22;
        await Assert.That((await GetStoredPlayerAsync(8560))["Energy"].AsInt32).IsEqualTo(expectedEnergy);
        await Assert.That((await GetStoredPlayerAsync(8561))["Energy"].AsInt32).IsEqualTo(expectedEnergy - 1);
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(expectedEnergy);
        await Assert.That((await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(expectedEnergy - 1);
    }

    [Test]
    public async Task Referral_ConcurrentFirstPosts_AppliesPerPlayerBonusesAsync()
    {
        using var inviterApi = Factory.CreateApiClient(8570);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        long[] friendIds = [8571, 8572, 8573, 8574];
        foreach (var friendId in friendIds)
        {
            using var friendApi = Factory.CreateApiClient(friendId);
            await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        }

        await Task.WhenAll(friendIds.Select(friendId => PublishAsync(friendId, "одновременный первый пост")));

        await Assert.That((await GetStoredPlayerAsync(8570))["Energy"].AsInt32).IsEqualTo(25);
        foreach (var friendId in friendIds)
        {
            var friend = await GetStoredPlayerAsync(friendId);
            await Assert.That(friend["Energy"].AsInt32).IsEqualTo(14);
            await Assert.That(friend["EnergyUsed"].AsInt32).IsEqualTo(1);
            var invite = await GetStoredInviteAsync(friend["_id"].AsString);
            await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsTrue();
            await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsTrue();
        }
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

        await Assert.That(response.Energy).IsEqualTo(20);
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

        var before = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await api.Player.DeleteCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(before - 1);
        var deletion = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Where(record => record.Category == typeof(ProductTelemetry).FullName)
            .Single(record => record.GetStructuredStateValue("player_id") == playerId &&
                record.GetStructuredStateValue("event_name") == "player_deleted");
        await Assert.That(deletion.GetStructuredStateValue("activity_kind")).IsEqualTo("lifecycle");

        await AssertCollectionHasNoPlayerAsync(
            "players",
            new BsonDocument("TelegramUserId", telegramUserId));
        foreach (var collection in new[]
                 {
                     "posts",
                     "publications",
                     "stars_memory",
                     "invites",
                     "challenges",
                     "events",
                     "analytics",
                     "worlds",
                 })
        {
            var ownerField = collection switch
            {
                "worlds" => "Player.PlayerId",
                "posts" => "AuthorId",
                _ => "PlayerId",
            };
            await AssertCollectionHasNoPlayerAsync(collection, new BsonDocument(ownerField, collection == "posts" ? $"Player_{playerId}" : playerId));
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

        await api.Player.UpdateSettingsAsync(
            new UpdateSettingsRequest { Address = Address.She },
            CancellationToken.None);
        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        using var publication = await api.Posts.GeneratePublicationAsync(
            await GetWorldIdAsync(api),
            new StartPublicationRequest { PublicationId = Guid.NewGuid().ToString("N"), Text = "Новый пост", OwnWords = true },
            CancellationToken.None);
        using var reader = new StreamReader(publication.Stream);
        await reader.ReadToEndAsync();
        using var profile = ParsePlayerLayer(GetCapturedChatMessages()[1]);
        await Assert.That(profile.RootElement.GetProperty("address").GetString()).IsEqualTo("she");
    }

    [Test]
    public async Task PlusInterest_BonusOnlyThenNotify_DoesNotRepeatBonusAsync()
    {
        using var api = Factory.CreateApiClient(8402);
        await CreatePlayerWithWorldAsync(api);
        var playerRepository = Factory.Services.GetRequiredService<IPlayerRepository>();
        foreach (var interest in new[] { false, true, true })
        {
            var response = await api.Player.ClaimPlusInterestAsync(new PlusInterestRequest { Interest = interest }, CancellationToken.None);
            await Assert.That(response.Bonus).IsEqualTo(interest ? 0 : 5);
            var saved = await playerRepository.GetPlayerAsync(TelegramUserId.From(8402), CancellationToken.None)
                        ?? throw new InvalidOperationException("Missing player.");
            await Assert.That(saved.PlusNotifyRequested).IsEqualTo(interest);
            var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
            await Assert.That(current.Energy).IsEqualTo(15);
        }

        using var legacyApi = Factory.CreateApiClient(8403);
        await CreatePlayerWithWorldAsync(legacyApi);
        await legacyApi.Player.ClaimPlusInterestAsync(null, CancellationToken.None);
        var legacyPlayer = await playerRepository.GetPlayerAsync(TelegramUserId.From(8403), CancellationToken.None)
                           ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(legacyPlayer.PlusNotifyRequested).IsTrue();
    }

    [Test]
    public async Task PlusInterest_ConcurrentClaims_GrantExactlyOneBonusAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8404);
        await CreatePlayerWithWorldAsync(api);

        // Act
        var calls = Enumerable.Range(0, 2).Select(_ =>
            api.Player.ClaimPlusInterestAsync(null, CancellationToken.None)).ToArray();
        var results = await Task.WhenAll(calls);

        // Assert
        var bonuses = results.Sum(result => result.Bonus);

        await Assert.That(bonuses).IsEqualTo(5);
        var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(current.Energy).IsEqualTo(15);
    }

    [Test]
    public async Task Settings_PartialUpdates_PreserveOtherFieldsAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8410);
        await CreatePlayerWithWorldAsync(api);
        var playerRepository = Factory.Services.GetRequiredService<IPlayerRepository>();
        var worldRepository = Factory.Services.GetRequiredService<IWorldRepository>();
        var userId = TelegramUserId.From(8410);
        var player = await playerRepository.GetPlayerAsync(userId, CancellationToken.None)
                     ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                Energy = 25,
                EnergyUsed = 2,
                Settings = player.Settings with { Plus = true },
            },
            cancellationToken: CancellationToken.None);

        var world = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await worldRepository.TryReplaceWorldAsync(
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
            api.Worlds.UpdateWorldAsync(
                await GetWorldIdAsync(api),
                DefinitionOf((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single()) with { Drama = Drama.Off },
                CancellationToken.None),
            api.Player.UpdateSettingsAsync(
                new UpdateSettingsRequest { Address = Address.She },
                CancellationToken.None));
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = false }, CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest(), CancellationToken.None);

        // Assert
        var saved = await playerRepository.GetPlayerAsync(userId, CancellationToken.None)
                    ?? throw new InvalidOperationException("Missing player.");
        var savedWorld = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Drama).IsEqualTo(Domain.Worlds.WorldDrama.Off);
        await Assert.That(saved.Address).IsEqualTo("she");
        await Assert.That(saved.Settings.Plus).IsFalse();
        await Assert.That(savedWorld.Player.Voice).IsEqualTo("Коротко и тепло");
        await Assert.That(saved.Settings.Notifications).IsEqualTo(player.Settings.Notifications);
        await Assert.That(saved.Settings.Quiet).IsEqualTo(player.Settings.Quiet);
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
        await Assert.That(saved.Energy).IsEqualTo(25);
        await Assert.That(saved.EnergyUsed).IsEqualTo(2);
    }

    [Test]
    public async Task Settings_MissingPlayer_ReturnsNotFoundAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8411);

        // Act
        var response = await Assert.ThrowsAsync<PlayerApiException>(() => api.Player.UpdateSettingsAsync(new UpdateSettingsRequest(), CancellationToken.None));
        await Assert.That(response).IsNotNull();

        // Assert
        await Assert.That((HttpStatusCode)response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        var playerRepository = Factory.Services.GetRequiredService<IPlayerRepository>();
        await Assert.That(await playerRepository.GetPlayerAsync(TelegramUserId.From(8411), CancellationToken.None)).IsNull();
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
        var playerRepository = Factory.Services.GetRequiredService<IPlayerRepository>();
        var worldRepository = Factory.Services.GetRequiredService<IWorldRepository>();
        var player = await playerRepository.GetPlayerAsync(TelegramUserId.From(userId), CancellationToken.None)
                     ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                PostsPeriodStartedAtUtc = player.PostsPeriodStartedAtUtc.AddDays(-1),
                EnergyUsed = 8,
                Energy = 25,
                PlusInterestClaimed = alreadyClaimed,
                PlusNotifyRequested = true,
            },
            cancellationToken: CancellationToken.None);
        var world = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await worldRepository.TryReplaceWorldAsync(
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
        await ResetGameDayAsync();

        // Act
        var calls = Enumerable.Range(0, 2).Select(_ => api.Player.ClaimPlusInterestAsync(
            new PlusInterestRequest { Interest = false }, CancellationToken.None)).ToArray();
        var results = await Task.WhenAll(calls);

        // Assert
        var bonuses = results.Sum(result => result.Bonus);

        await Assert.That(bonuses).IsEqualTo(alreadyClaimed ? 0 : 5);
        var saved = await playerRepository.GetPlayerAsync(TelegramUserId.From(userId), CancellationToken.None)
                    ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(saved.PlusInterestClaimed).IsTrue();
        await Assert.That(saved.PlusNotifyRequested).IsTrue();
        await Assert.That(saved.Energy).IsEqualTo(alreadyClaimed ? 10 : 15);
        await Assert.That(saved.EnergyUsed).IsEqualTo(0);
        await Assert.That(saved.PostsPeriodStartedAtUtc).IsEqualTo(player.PostsPeriodStartedAtUtc);
        _ = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single(item => item.Id == world.Id.Value);
        var savedWorld = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Wants.All(want => !want.Done)).IsTrue();
        await Assert.That(savedWorld.Stars.All(relation => relation.RelationGainedInPeriod == 0 &&
                                                           relation.RelationPeriodStartedAtUtc ==
                                                           player.PostsPeriodStartedAtUtc)).IsTrue();
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
    }

    [Test]
    public async Task Referral_RepeatedModeration_GrantsEachSideOnceAsync()
    {
        // Arrange
        using var inviterApi = Factory.CreateApiClient(8501);
        using var friendApi = Factory.CreateApiClient(8502);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var generation = await GenerateAsync(8502, "первый пост");
        var worldId = await GetWorldIdAsync(friendApi);
        var usedAt = Factory.Session.Clock.GetUtcNow();

        // Act
        var first = await friendApi.Posts.ModeratePublicationAsync(
            generation.PublicationId,
            worldId,
            CancellationToken.None);
        var repeated =
            await friendApi.Posts.ModeratePublicationAsync(generation.PublicationId, worldId, CancellationToken.None);

        // Assert
        var postId = first.OfType<ResultLiveEvent>().Single().Result.PostId;
        await Assert.That(repeated.OfType<ResultLiveEvent>().Single().Result.PostId)
            .IsEqualTo(postId);
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(15);
        await Assert.That((await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(14);
        var invite = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From(friend.Id)).SingleAsync();
        await Assert.That(invite.InviterBonusUsedAtUtc).IsEqualTo(usedAt);
        await Assert.That(invite.InviteeBonusUsedAtUtc).IsEqualTo(usedAt);
        await Assert.That(await Database.GetCollection<BsonDocument>("posts").CountDocumentsAsync(
            new BsonDocument("AuthorId", $"Player_{friend.Id}"))).IsEqualTo(1L);
        Factory.Session.Clock.SetUtcNow(usedAt.AddDays(1));
        await ResetGameDayAsync();
        await friendApi.Posts.ModeratePublicationAsync(generation.PublicationId, worldId, CancellationToken.None);
        var saved = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From(friend.Id)).SingleAsync();
        await Assert.That(saved.InviterBonusUsedAtUtc).IsEqualTo(usedAt);
        await Assert.That(saved.InviteeBonusUsedAtUtc).IsEqualTo(usedAt);
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(10);
        await Assert.That((await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(10);
    }

    [Test]
    [Arguments(-1)]
    [Arguments(0)]
    public async Task Referral_DayBoundary_UsesSixUtcResetAsync(int secondsAfterReset)
    {
        // Arrange
        var reset = new DateTimeOffset(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);
        Factory.Session.Clock.SetUtcNow(reset.AddSeconds(-2));
        using var inviterApi = Factory.CreateApiClient(8520);
        using var friendApi = Factory.CreateApiClient(8521);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        await PublishAsync(8521, "пост перед сбросом");
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(15);

        // Act
        Factory.Session.Clock.SetUtcNow(reset.AddSeconds(secondsAfterReset));
        await ResetGameDayAsync();
        var currentInviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var currentFriend = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(currentInviter.Energy).IsEqualTo(secondsAfterReset < 0 ? 15 : 10);
        await Assert.That(currentFriend.Energy).IsEqualTo(secondsAfterReset < 0 ? 14 : 10);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
        var invite = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From(friend.Id)).SingleAsync();
        await Assert.That(invite.InviterBonusUsed).IsTrue();
        await Assert.That(invite.InviteeBonusUsed).IsTrue();
        await Assert.That(invite.InviterBonusUsedAtUtc).IsEqualTo(reset.AddSeconds(-2));
        await Assert.That(invite.InviteeBonusUsedAtUtc).IsEqualTo(reset.AddSeconds(-2));
    }

    [Test]
    public async Task Referral_FailedGeneration_PreservesBonusForSuccessfulRetryAsync()
    {
        // Arrange
        using var inviterApi = Factory.CreateApiClient(8530);
        using var friendApi = Factory.CreateApiClient(8531);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var unsafeResponse = CreateCommentResponse(false);
        var call = unsafeResponse.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
            .Single();
        (call.Arguments ?? throw new InvalidOperationException("Missing arguments."))["text"] = "наркотики";
        Factory.Llm.Respond(unsafeResponse);

        // Act
        var failed = await GenerateAsync(8531, "безопасный пост");

        // Assert
        await Assert.That(failed.Body).Contains("\"type\":\"failed\"");
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(10);
        var pending = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(pending.Energy).IsEqualTo(10);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(5);
        var invite = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From(friend.Id)).SingleAsync();
        await Assert.That(invite.InviterBonusUsed).IsFalse();
        await Assert.That(invite.InviteeBonusUsed).IsFalse();
        await Assert.That(invite.InviterBonusUsedAtUtc).IsNull();
        await Assert.That(invite.InviteeBonusUsedAtUtc).IsNull();
        await Assert.That(await Database.GetCollection<BsonDocument>("posts").CountDocumentsAsync(
            new BsonDocument("AuthorId", $"Player_{friend.Id}"))).IsEqualTo(0L);
        Factory.Llm.Respond(CreateChatResponse);
        await PublishAsync(8531, "успешный первый пост");
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(15);
        var success = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(success.Energy).IsEqualTo(14);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
    }

    [Test]
    public async Task Referral_FirstPostAtDayReset_GrantsBothBonusesInNewPeriodAsync()
    {
        // Arrange
        using var inviterApi = Factory.CreateApiClient(8550);
        using var friendApi = Factory.CreateApiClient(8551);
        await CreatePlayerWithWorldAsync(inviterApi);
        var referral = await inviterApi.Player.GetReferralCodeAsync(CancellationToken.None);
        var friend = await CreatePlayerWithWorldAsync(friendApi, referralCode: referral.Code);
        var reset = new DateTimeOffset(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);
        Factory.Session.Clock.SetUtcNow(reset);
        await ResetGameDayAsync();

        // Act
        await PublishAsync(8551, "первый пост на границе дня");

        // Assert
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(15);
        var current = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(current.Energy).IsEqualTo(14);
        var invitation = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == PlayerId.From(friend.Id)).SingleAsync();
        await Assert.That(invitation.InviterBonusUsedAtUtc).IsEqualTo(reset);
        await Assert.That(invitation.InviteeBonusUsedAtUtc).IsEqualTo(reset);
    }

    [Test]
    public async Task Settings_PlusTransitions_PreserveSpentEnergyAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8580);
        var response = await CreatePlayerWithWorldAsync(api);
        var players = Database.GetCollection<PlayerProfile>("players");
        await players.UpdateOneAsync(
            item => item.Id == PlayerId.From(response.Id),
            Builders<PlayerProfile>.Update.Set(item => item.Energy, 6).Set(item => item.EnergyUsed, 4));

        // Act
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = true }, CancellationToken.None);
        var enabled = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = true }, CancellationToken.None);
        var repeated = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = false }, CancellationToken.None);
        var disabled = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(enabled.Energy).IsEqualTo(16);
        await Assert.That(repeated.Energy).IsEqualTo(16);
        await Assert.That(disabled.Energy).IsEqualTo(16);
        var stored = await GetStoredPlayerAsync(8580);
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(4);
        await Assert.That(stored.Contains("DailyBonus")).IsFalse();
        await Assert.That(stored.Contains("ChallengeDailyBonus")).IsFalse();
        Factory.Session.Clock.SetUtcNow(Factory.Session.Clock.GetUtcNow().AddDays(1));
        await ResetGameDayAsync();
        var reset = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(reset.Energy).IsEqualTo(10);
        await Assert.That((await GetStoredPlayerAsync(8580))["EnergyUsed"].AsInt32).IsEqualTo(0);
    }

    [Test]
    public async Task PlusInterest_ExhaustedProgression_BonusDoesNotRestoreSkillRewardsAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8581);
        var response = await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            item => item.Id == PlayerId.From(response.Id),
            Builders<PlayerProfile>.Update.Set(item => item.Energy, 0).Set(item => item.EnergyUsed, 10));
        var before = await GetStoredPlayerAsync(8581);

        // Act
        await api.Player.ClaimPlusInterestAsync(new PlusInterestRequest { Interest = false }, CancellationToken.None);
        await PublishAsync(8581, "пост после бонуса");

        // Assert
        var stored = await GetStoredPlayerAsync(8581);
        await Assert.That(stored["Energy"].AsInt32).IsEqualTo(4);
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(11);
        await Assert.That(stored["Skills"]).IsEqualTo(before["Skills"]);
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy).IsEqualTo(4);
    }

    [Test]
    public async Task Me_EnergyContract_ExposesStoredRemainingEnergyOnlyAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8582);
        var player = await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            item => item.Id == PlayerId.From(player.Id),
            Builders<PlayerProfile>.Update.Set(item => item.Energy, 7).Set(item => item.EnergyUsed, 3));

        // Act
        var response = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(response.Energy).IsEqualTo(7);
    }

    [Test]
    public async Task Invitation_WithoutAuthentication_ReturnsUnauthorizedAsync()
    {
        using var api = Factory.CreateApiClient();
        var response = await Assert.ThrowsAsync<PlayerApiException>(() => api.Player.GetInvitationAsync(CancellationToken.None));
        await Assert.That(response).IsNotNull();
        await Assert.That((HttpStatusCode)response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Invitation_BeforeOnboarding_ReturnsNotFoundAsync()
    {
        using var api = Factory.CreateApiClient(8583);
        var response = await Assert.ThrowsAsync<PlayerApiException>(() => api.Player.GetInvitationAsync(CancellationToken.None));
        await Assert.That(response).IsNotNull();
        await Assert.That((HttpStatusCode)response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    private Task<BsonDocument> GetStoredInviteAsync(string inviteeId) =>
        Database.GetCollection<BsonDocument>("invites").Find(new BsonDocument("InviteeId", inviteeId))
            .SingleAsync(CancellationToken.None);
}
