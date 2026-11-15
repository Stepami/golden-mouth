using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using Reki.Api.Requests.Player;
using Reki.Api.Requests.Telemetry.AnalyticsBatch;
using Reki.Application.Player;
using Reki.Application.Player.Responses;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Invites;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class PlayerControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    [Arguments(119, false)]
    [Arguments(120, true)]
    public async Task Me_Activity_ReschedulesAtTwoHoursAsync(int minutes, bool rescheduled)
    {
        using var api = Factory.CreateApiClient(18302);
        var response = await api.RegisterAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick },
            CancellationToken.None);
        var now = Factory.Session.Clock.GetUtcNow();
        var players = Database.GetCollection<PlayerProfile>("players");
        var notifications = Database.GetCollection<Notification>("notifications");
        var pending = await notifications
            .Find(item => item.PlayerId == response.Id && item.Status == NotificationStatus.Pending)
            .SingleAsync();
        var initialSendAt = now.AddDays(2);
        await notifications.ReplaceOneAsync(item => item.Id == pending.Id, pending with { SendAt = initialSendAt });
        await players.UpdateOneAsync(
            item => item.Id == response.Id,
            Builders<PlayerProfile>.Update.Set(item => item.LastSeenAt, now.AddMinutes(-minutes)));
        Factory.Services.GetRequiredService<IMemoryCache>().Remove(response.Id);

        await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        var saved = await notifications.Find(item => item.Id == pending.Id).SingleAsync();
        var player = await players.Find(item => item.Id == response.Id).SingleAsync();
        await Assert.That(saved.SendAt).IsEqualTo(rescheduled ? now.AddDays(1) : initialSendAt);
        await Assert.That(player.LastSeenAt).IsEqualTo(now);
        await Assert
            .That(await notifications.CountDocumentsAsync(item =>
                item.PlayerId == response.Id && item.Status == NotificationStatus.Pending)).IsEqualTo(1L);
    }

    [Test]
    public async Task Me_WithoutNotificationChannels_DoesNotScheduleAsync()
    {
        using var api = Factory.CreateApiClient(18304);
        var response = await api.RegisterAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick },
            CancellationToken.None);
        var players = Database.GetCollection<PlayerProfile>("players");
        var notifications = Database.GetCollection<Notification>("notifications");
        await notifications.DeleteManyAsync(item => item.PlayerId == response.Id);
        await players.UpdateOneAsync(
            item => item.Id == response.Id,
            Builders<PlayerProfile>.Update.Set(item => item.NotificationChannels, Array.Empty<PlayerNotificationChannel>()));
        Factory.Services.GetRequiredService<IMemoryCache>().Remove(response.Id);

        await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        await Assert.That(await notifications.CountDocumentsAsync(item => item.PlayerId == response.Id)).IsEqualTo(0L);
    }

    [Test]
    public async Task Me_CachedActivity_DoesNotUpdatePlayerOrScheduleAsync()
    {
        using var api = Factory.CreateApiClient(18303);
        var response = await api.RegisterAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick },
            CancellationToken.None);
        var now = Factory.Session.Clock.GetUtcNow();
        var players = Database.GetCollection<PlayerProfile>("players");
        var notifications = Database.GetCollection<Notification>("notifications");
        var pending = await notifications
            .Find(item => item.PlayerId == response.Id && item.Status == NotificationStatus.Pending)
            .SingleAsync();
        await players.UpdateOneAsync(
            item => item.Id == response.Id,
            Builders<PlayerProfile>.Update.Set(item => item.LastSeenAt, now.AddHours(-3)));

        await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        var player = await players.Find(item => item.Id == response.Id).SingleAsync();
        var saved = await notifications.Find(item => item.Id == pending.Id).SingleAsync();
        await Assert.That(player.LastSeenAt).IsEqualTo(now.AddHours(-3));
        await Assert.That(saved.SendAt).IsEqualTo(pending.SendAt);
    }

    [Test]
    public async Task Me_RegisteredPlayer_ReturnsProfileAsync()
    {
        using var api = Factory.CreateApiClient(18301);
        await api.RegisterAsync(new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick }, CancellationToken.None);

        var player = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        await Assert.That(player.Energy).IsEqualTo(10);
        await Assert.That(player.Skills.Count).IsEqualTo(3);
        await Assert.That(await Database.GetCollection<BsonDocument>("players")
            .CountDocumentsAsync(new BsonDocument("ExternalUserIds", "telegram:18301"), cancellationToken: CancellationToken.None))
            .IsEqualTo(1L);
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
    public async Task Onboarding_WithoutAuthentication_DoesNotCreatePlayerAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient();
        var players = Database.GetCollection<BsonDocument>("players");
        var before = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);

        // Act
        int? statusCode = null;
        try
        {
            await new AuthClient(Factory.CreateClient()).CompleteOnboardingAsync(
                new OnboardingRequest
                {
                    BirthGroup = BirthGroup.Adult,
                    BirthYear = 1990,
                    Address = Address.Nick,
                },
                CancellationToken.None);
        }
        catch (AuthApiException caught)
        {
            statusCode = caught.StatusCode;
        }

        // Assert
        await Assert.That(statusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        using var malformedClient = Factory.CreateClient();
        malformedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "not-base64!");
        var malformed = await Assert.ThrowsAsync<AuthApiException>(() =>
            new AuthClient(malformedClient).CompleteOnboardingAsync(
                new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick }, CancellationToken.None));
        await Assert.That(malformed?.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
        var after = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    public async Task Onboarding_WithBirthYear_PersistsYearAndReturnsCurrentBandAsync()
    {
        const long userId = 10101;
        using var api = Factory.CreateApiClient(userId);
        await api.RegisterAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Adult,
                BirthYear = 1970,
                Address = Address.Nick,
            },
            CancellationToken.None);
        var profile = await GetStoredPlayerAsync(userId);
        await Assert.That(profile["BirthYear"].AsInt32).IsEqualTo(1970);
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).AgeBand).IsEqualTo("45+");
        await Assert.That(profile["BirthGroup"].AsString).IsEqualTo("18+");
    }

    [Test]
    public async Task Onboarding_WithEnumContracts_PersistsYearWithoutExactDateAsync()
    {
        const long telegramUserId = 1001;
        using var api = Factory.CreateApiClient(telegramUserId);

        var response = await api.RegisterAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Adult,
                BirthYear = 1990,
                Address = Address.Nick,
            },
            CancellationToken.None);

        await Assert.That(response.Energy).IsEqualTo(10);
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).Id).IsEqualTo(response.Id);
        await Assert.That((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Count).IsEqualTo(0);

        var player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("ExternalUserIds", $"telegram:{telegramUserId}"))
            .SingleAsync();
        await Assert.That(player["_id"].AsBsonBinaryData.SubType).IsEqualTo(BsonBinarySubType.UuidStandard);
        await Assert.That(player["ExternalUserIds"][0].BsonType).IsEqualTo(BsonType.String);
        await Assert.That(player.Contains("World")).IsFalse();
        await Assert.That(player["Skills"].AsBsonDocument.Names)
            .IsEquivalentTo(["brave", "humor", "topic"]);
        await Assert.That(player["BirthGroup"].AsString).IsEqualTo("18+");
        await Assert.That(player.Names.Any(name => name.Contains("date", StringComparison.OrdinalIgnoreCase)))
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
        var invite = await GetStoredInviteAsync(friend["_id"].AsBsonBinaryData.ToGuid().ToString("N"));
        await Assert.That(invite["InviterId"]).IsEqualTo(inviter["_id"]);
        await Assert.That(friend.Contains("InvitedBy")).IsFalse();
        await Assert.That(friend["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(friend.Contains("FirstPostInviteBonus")).IsFalse();
        await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsFalse();
        await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsFalse();
        await Assert.That(invite["InviteeBonusUsedAtUtc"].IsBsonNull).IsTrue();
        await Assert.That(invite["InviterBonusUsedAtUtc"].IsBsonNull).IsTrue();
        await Assert.That(inviter["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
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
            await Assert.That(player["Wallet"]["Energy"].AsInt32).IsEqualTo(userId == 8004 ? 9 : 10);
            await Assert.That(player.Contains("FirstPostInviteBonus")).IsFalse();
        }

        var noInviter = await unknownApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(noInviter.Energy).IsEqualTo(9);
        await Assert.That((await unknownApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
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
        await Assert.That(pendingInvite.Bonus).IsEqualTo(1);
        await Assert.That(inviterAfterOnboarding.Energy).IsEqualTo(10);
        await Assert.That(inviterAfterOnboarding.HitOwnWorldMilestone).IsFalse();
        await Assert.That(inviter.HitOwnWorldMilestone).IsTrue();
        await Assert.That(inviter.Energy).IsEqualTo(10);
        await Assert.That(friend.Energy).IsEqualTo(9);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
        await Assert.That(storedInviter["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(storedFriend["Wallet"]["Energy"].AsInt32).IsEqualTo(8);
        await Assert.That(storedInviter["Wallet"]["Crystals"].AsInt32).IsEqualTo(13);
        await Assert.That(storedFriend["Wallet"]["Crystals"].AsInt32).IsEqualTo(11);
        var invite = await GetStoredInviteAsync(storedFriend["_id"].AsBsonBinaryData.ToGuid().ToString("N"));
        await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsTrue();
        await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsTrue();
        await Assert.That(invite["InviterBonusUsedAtUtc"]).IsEqualTo(invite["InviteeBonusUsedAtUtc"]);
    }

    [Test]
    public async Task Referral_FourFriendsPublish_GrantsEveryInviterBonusAsync()
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
        await Assert.That(inviter.Energy).IsEqualTo(10);
        await Assert.That(storedInviter.Contains("InviteBonusesEarnedInPeriod")).IsFalse();
        await Assert.That(storedInviter["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(storedInviter["Wallet"]["Crystals"].AsInt32).IsEqualTo(22);
        await Assert.That(fourthFriend["Wallet"]["Energy"].AsInt32).IsEqualTo(9);
        await Assert.That(fourthFriend["Wallet"]["Crystals"].AsInt32).IsEqualTo(11);
        using var fourthApi = Factory.CreateApiClient(8016);
        await Assert.That((await fourthApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy).IsEqualTo(9);
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
            Builders<BsonDocument>.Filter.In("_id", new[] { inviter.Id, friend.Id }.Select(id => new BsonBinaryData(id.Value, GuidRepresentation.Standard))),
            Builders<BsonDocument>.Update.Set("Wallet.Energy", 4));
        if (alreadyUsed)
        {
            var now = Factory.Session.Clock.GetUtcNow();
            await Database.GetCollection<Invitation>("invites").UpdateOneAsync(
                item => item.InviteeId == friend.Id,
                Builders<Invitation>.Update
                    .Set(item => item.InviteeBonusUsed, true)
                    .Set(item => item.InviterBonusUsed, true)
                    .Set(item => item.InviteeBonusUsedAtUtc, now)
                    .Set(item => item.InviterBonusUsedAtUtc, now));
        }

        await PublishAsync(8561, "пост с существующим бонусом");

        var expectedCrystals = alreadyUsed ? 10 : 11;
        await Assert.That((await GetStoredPlayerAsync(8560))["Wallet"]["Energy"].AsInt32).IsEqualTo(4);
        await Assert.That((await GetStoredPlayerAsync(8561))["Wallet"]["Energy"].AsInt32).IsEqualTo(3);
        await Assert.That((await GetStoredPlayerAsync(8561))["Wallet"]["Crystals"].AsInt32).IsEqualTo(expectedCrystals);
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(4);
        await Assert.That((await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy)
            .IsEqualTo(3);
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

        await Assert.That(response.Energy).IsEqualTo(10);
    }

    [Test]
    public async Task DeleteAccount_RemovesPlayerDataFromMongoAsync()
    {
        // Arrange
        const long telegramUserId = 1004;
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);

        var players = Database.GetCollection<BsonDocument>("players");
        var player = await players
            .Find(new BsonDocument("ExternalUserIds", $"telegram:{telegramUserId}"))
            .SingleAsync();
        var playerId = player["_id"].AsBsonBinaryData.ToGuid().ToString("N");

        await PublishAsync(telegramUserId, "hello");
        await api.Telemetry.SubmitAnalyticsAsync(
            new AnalyticsBatchRequest
            {
                Session = SessionId.New(),
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
        var invitations = Database.GetCollection<Invitation>("invites");
        var deletedPlayerId = PlayerId.From(Guid.Parse(playerId));
        var otherPlayerId = PlayerId.New();
        var authOptions = Factory.Services.GetRequiredService<IOptions<OpenIddictMongoDbOptions>>().Value;
        var tokens = Database.GetCollection<OpenIddictMongoDbToken>(authOptions.TokensCollectionName);
        var authorizations = Database.GetCollection<OpenIddictMongoDbAuthorization>(authOptions.AuthorizationsCollectionName);
        var applications = Database.GetCollection<OpenIddictMongoDbApplication>(authOptions.ApplicationsCollectionName);
        var otherSubject = otherPlayerId.ToString();
        await authorizations.InsertManyAsync(new[]
        {
            new OpenIddictMongoDbAuthorization { Subject = playerId, Status = OpenIddictConstants.Statuses.Valid },
            new OpenIddictMongoDbAuthorization { Subject = playerId, Status = OpenIddictConstants.Statuses.Revoked },
            new OpenIddictMongoDbAuthorization { Subject = otherSubject, Status = OpenIddictConstants.Statuses.Valid },
        });
        await tokens.InsertManyAsync(new[]
        {
            new OpenIddictMongoDbToken { Subject = playerId, Status = OpenIddictConstants.Statuses.Valid, Type = OpenIddictConstants.TokenTypeIdentifiers.RefreshToken, ReferenceId = Guid.NewGuid().ToString("N") },
            new OpenIddictMongoDbToken { Subject = playerId, Status = OpenIddictConstants.Statuses.Valid, Type = OpenIddictConstants.TokenTypeIdentifiers.AccessToken, ReferenceId = Guid.NewGuid().ToString("N") },
            new OpenIddictMongoDbToken { Subject = otherSubject, Status = OpenIddictConstants.Statuses.Valid, Type = OpenIddictConstants.TokenTypeIdentifiers.RefreshToken, ReferenceId = Guid.NewGuid().ToString("N") },
        });
        var applicationCount = await applications.CountDocumentsAsync(item => item.ClientId == "reki-test-client");
        await Assert.That(applicationCount).IsEqualTo(1L);
        var unrelatedInvitation = Invitation.Create(otherPlayerId, PlayerId.New(), Factory.Session.Clock.GetUtcNow(), 5);
        await invitations.InsertManyAsync(new[]
        {
            Invitation.Create(deletedPlayerId, otherPlayerId, Factory.Session.Clock.GetUtcNow(), 5),
            Invitation.Create(PlayerId.New(), deletedPlayerId, Factory.Session.Clock.GetUtcNow(), 5),
            unrelatedInvitation,
        });

        var before = await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None);

        // Act
        await api.Player.DeleteCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(await tokens.CountDocumentsAsync(item => item.Subject == playerId)).IsEqualTo(0L);
        await Assert.That(await authorizations.CountDocumentsAsync(item => item.Subject == playerId)).IsEqualTo(0L);
        await Assert.That(await tokens.CountDocumentsAsync(item => item.Subject == otherSubject)).IsEqualTo(1L);
        await Assert.That(await authorizations.CountDocumentsAsync(item => item.Subject == otherSubject)).IsEqualTo(1L);
        await Assert.That(await applications.CountDocumentsAsync(item => item.ClientId == "reki-test-client")).IsEqualTo(applicationCount);
        var savedInvitations = await invitations.Find(FilterDefinition<Invitation>.Empty).ToListAsync();
        await Assert.That(savedInvitations).IsEquivalentTo(new[] { unrelatedInvitation });
        await Assert.That(await players.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(before - 1);
        var deletion = Factory.Services.GetFakeLogCollector().GetSnapshot()
            .Where(record => record.Category == typeof(ProductTelemetry).FullName)
            .Single(record => record.GetStructuredStateValue("player_id") == playerId &&
                record.GetStructuredStateValue("event_name") == "player_deleted");
        await Assert.That(deletion.GetStructuredStateValue("activity_kind")).IsEqualTo("lifecycle");

        await AssertCollectionHasNoPlayerAsync(
            "players",
            new BsonDocument("ExternalUserIds", $"telegram:{telegramUserId}"));
        foreach (var collection in new[]
                 {
                     "posts",
                     "publications",
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
        await api.RegisterAsync(
            new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick },
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
        await PublishAsync(8400, "Новый пост");
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
            var saved = await playerRepository.GetPlayerAsync(ExternalUserId.From($"telegram:{8402}"), CancellationToken.None)
                        ?? throw new InvalidOperationException("Missing player.");
            await Assert.That(saved.PlusNotifyRequested).IsEqualTo(interest);
            var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
            await Assert.That(current.Energy).IsEqualTo(10);
            await Assert.That(current.EnergyWallet?.Crystals).IsEqualTo(15);
        }

        using var legacyApi = Factory.CreateApiClient(8403);
        await CreatePlayerWithWorldAsync(legacyApi);
        await legacyApi.Player.ClaimPlusInterestAsync(null, CancellationToken.None);
        var legacyPlayer = await playerRepository.GetPlayerAsync(ExternalUserId.From($"telegram:{8403}"), CancellationToken.None)
                           ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(legacyPlayer.PlusNotifyRequested).IsTrue();
    }

    [Test]
    public async Task Settings_PartialUpdates_PreserveOtherFieldsAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8410);
        await CreatePlayerWithWorldAsync(api);
        var playerRepository = Factory.Services.GetRequiredService<IPlayerRepository>();
        var worldRepository = Factory.Services.GetRequiredService<IWorldRepository>();
        var userId = ExternalUserId.From($"telegram:{8410}");
        var player = await playerRepository.GetPlayerAsync(userId, CancellationToken.None)
                     ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                Wallet = player.Wallet with { Energy = 8 },
                Settings = player.Settings with { Plus = true },
            },
            cancellationToken: CancellationToken.None);

        var world = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await worldRepository.ReplaceWorldAsync(
            world with
            {
                Version = world.Version + 1,
                Player = world.Player with
                {
                    Subscribers = 1234,
                    Voice = "Коротко и тепло",
                },
            },
            CancellationToken.None);

        // Act
        await api.Worlds.UpdateWorldAsync(
            await GetWorldIdAsync(api),
            DefinitionOf((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single()) with { Drama = WorldDrama.Off },
            CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Address = Address.She }, CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = false }, CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest(), CancellationToken.None);

        // Assert
        var saved = await playerRepository.GetPlayerAsync(userId, CancellationToken.None)
                    ?? throw new InvalidOperationException("Missing player.");
        var savedWorld = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Drama).IsEqualTo(WorldDrama.Off);
        await Assert.That(saved.Address).IsEqualTo(Address.She);
        await Assert.That(saved.Settings.Plus).IsFalse();
        await Assert.That(savedWorld.Player.Voice).IsEqualTo("Коротко и тепло");
        await Assert.That(saved.Settings.Notifications).IsEqualTo(player.Settings.Notifications);
        await Assert.That(saved.Settings.Quiet).IsEqualTo(player.Settings.Quiet);
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
        await Assert.That(saved.Wallet.Energy).IsEqualTo(8);
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
        await Assert.That(await playerRepository.GetPlayerAsync(ExternalUserId.From($"telegram:{8411}"), CancellationToken.None)).IsNull();
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
        var player = await playerRepository.GetPlayerAsync(ExternalUserId.From($"telegram:{userId}"), CancellationToken.None)
                     ?? throw new InvalidOperationException("Missing player.");
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(
            item => item.Id == player.Id,
            player with
            {
                Wallet = player.Wallet with { Energy = 4 },
                PlusInterestClaimed = alreadyClaimed,
                PlusNotifyRequested = true,
            },
            cancellationToken: CancellationToken.None);
        var skills = (await GetStoredPlayerAsync(userId))["Skills"].DeepClone();
        var world = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await worldRepository.ReplaceWorldAsync(
            world with
            {
                Version = world.Version + 1,
                Player = world.Player with
                {
                    Subscribers = 1234,
                },
            },
            CancellationToken.None);

        // Act
        var response = await api.Player.ClaimPlusInterestAsync(new PlusInterestRequest { Interest = false }, CancellationToken.None);

        // Assert
        await Assert.That(response.Bonus).IsEqualTo(alreadyClaimed ? 0 : 5);
        var saved = await playerRepository.GetPlayerAsync(ExternalUserId.From($"telegram:{userId}"), CancellationToken.None)
                    ?? throw new InvalidOperationException("Missing player.");
        await Assert.That(saved.PlusInterestClaimed).IsTrue();
        await Assert.That(saved.PlusNotifyRequested).IsTrue();
        await Assert.That(saved.Wallet.Energy).IsEqualTo(4);
        await Assert.That(saved.Wallet.Crystals).IsEqualTo(alreadyClaimed ? 10 : 15);
        await Assert.That((await GetStoredPlayerAsync(userId))["Skills"]).IsEqualTo(skills);
        _ = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single(item => item.Id == world.Id);
        var savedWorld = (await worldRepository.GetWorldsAsync(player.Id, CancellationToken.None)).Single();
        await Assert.That(savedWorld.Player.Subscribers).IsEqualTo(1234);
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
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy).IsEqualTo(10);

        // Act
        Factory.Session.Clock.SetUtcNow(reset.AddSeconds(secondsAfterReset));
        var currentInviter = await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var currentFriend = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(currentInviter.Energy).IsEqualTo(10);
        await Assert.That(currentFriend.Energy).IsEqualTo(9);
        await Assert.That((await friendApi.Player.GetInvitationAsync(CancellationToken.None)).Bonus).IsEqualTo(0);
        var invite = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == friend.Id).SingleAsync();
        await Assert.That(invite.InviterBonusUsed).IsTrue();
        await Assert.That(invite.InviteeBonusUsed).IsTrue();
        await Assert.That(invite.InviterBonusUsedAtUtc).IsEqualTo(reset.AddSeconds(-2));
        await Assert.That(invite.InviteeBonusUsedAtUtc).IsEqualTo(reset.AddSeconds(-2));
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

        // Act
        await PublishAsync(8551, "первый пост на границе дня");

        // Assert
        await Assert.That((await inviterApi.Player.GetCurrentPlayerAsync(CancellationToken.None)).Energy).IsEqualTo(10);
        var current = await friendApi.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(current.Energy).IsEqualTo(9);
        var invitation = await Database.GetCollection<Invitation>("invites")
            .Find(item => item.InviteeId == friend.Id).SingleAsync();
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
            item => item.Id == response.Id,
            Builders<PlayerProfile>.Update.Set(item => item.Wallet.Energy, 6));

        // Act
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = true }, CancellationToken.None);
        var enabled = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = true }, CancellationToken.None);
        var repeated = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await api.Player.UpdateSettingsAsync(new UpdateSettingsRequest { Plus = false }, CancellationToken.None);
        var disabled = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(enabled.Energy).IsEqualTo(6);
        await Assert.That(repeated.Energy).IsEqualTo(6);
        await Assert.That(disabled.Energy).IsEqualTo(6);
        await Assert.That(enabled.EnergyWallet?.Crystals).IsEqualTo(20);
        await Assert.That(repeated.EnergyWallet?.Crystals).IsEqualTo(20);
        await Assert.That(disabled.EnergyWallet?.Crystals).IsEqualTo(20);
        var stored = await GetStoredPlayerAsync(8580);
        await Assert.That(stored.Contains("DailyBonus")).IsFalse();
        await Assert.That(stored.Contains("ChallengeDailyBonus")).IsFalse();
        Factory.Session.Clock.SetUtcNow(Factory.Session.Clock.GetUtcNow().AddDays(1));
        var reset = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(reset.Energy).IsEqualTo(6);
    }

    [Test]
    public async Task Me_EnergyContract_ExposesStoredRemainingEnergyOnlyAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(8582);
        var player = await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            item => item.Id == player.Id,
            Builders<PlayerProfile>.Update.Set(item => item.Wallet.Energy, 7));

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
    public async Task Invitation_BeforeOnboarding_ReturnsEmptyAsync()
    {
        using var api = Factory.CreateApiClient(8583);
        await api.RegisterAsync(new OnboardingRequest { BirthGroup = BirthGroup.Adult, BirthYear = 1990, Address = Address.Nick }, CancellationToken.None);
        var response = await api.Player.GetInvitationAsync(CancellationToken.None);
        await Assert.That(response.Bonus).IsEqualTo(0);
    }

    [Test]
    public async Task Onboarding_NewWorlds_KeepAccountWalletAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28401);
        var onboarded = await CreatePlayerWithWorldAsync(api);

        // Act
        var initial = await ReadWalletAsync(api);
        await SetWalletAsync(28401, 4, 7);
        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var after = await ReadWalletAsync(api);
        var worlds = await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);

        // Assert
        await Assert.That(onboarded.Energy).IsEqualTo(10);
        await Assert.That(initial.Amount).IsEqualTo(10);
        await Assert.That(initial.Crystals).IsEqualTo(10);
        await Assert.That(initial.Cap).IsEqualTo(10);
        await Assert.That(initial.RegenSeconds).IsEqualTo(600);
        await Assert.That(initial.NextEnergyAtUtc).IsNull();
        await Assert.That(after.Amount).IsEqualTo(4);
        await Assert.That(after.Crystals).IsEqualTo(7);
        await Assert.That(worlds.Count).IsEqualTo(2);
        await Assert.That(worlds.SelectMany(world => world.Stars).All(star => star.ChannelSubscribers is null)).IsTrue();
    }

    [Test]
    [Arguments(3, 0)]
    [Arguments(10, 7)]
    public async Task Exchange_PartialEnergy_FillsToTenWithOneDebitAsync(int crystals, int remainingCrystals)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28405);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28405, 9, crystals);
        var before = await GetStoredPlayerAsync(28405);

        // Act
        var result = await api.Player.ExchangeEnergyAsync(CancellationToken.None);
        var after = await GetStoredPlayerAsync(28405);

        // Assert
        await Assert.That(result.Exchanged).IsTrue();
        await Assert.That(result.Wallet).IsNotNull();
        await Assert.That(result.Wallet.Amount).IsEqualTo(10);
        await Assert.That(result.Wallet.Crystals).IsEqualTo(remainingCrystals);
        await Assert.That(result.Wallet.NextEnergyAtUtc).IsNull();
        await Assert.That(after["Skills"]).IsEqualTo(before["Skills"]);
        await Assert.That(after["Settings"]).IsEqualTo(before["Settings"]);
        await Assert.That(after.Contains("EnergyExchangeRequestIds")).IsFalse();
    }

    [Test]
    [Arguments(10, 10, "energy_full")]
    [Arguments(9, 2, "insufficient_crystals")]
    public async Task Exchange_Unavailable_ReturnsExpectedResultWithoutDebitAsync(int energy, int crystals, string reason)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28406);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28406, energy, crystals);
        var before = await GetStoredPlayerAsync(28406);

        // Act
        var result = await api.Player.ExchangeEnergyAsync(CancellationToken.None);
        var wallet = await ReadWalletAsync(api);
        var after = await GetStoredPlayerAsync(28406);

        // Assert
        await Assert.That(result.Exchanged).IsFalse();
        await Assert.That(result.Wallet).IsNull();
        await Assert.That(wallet.ExchangeQuote.Available).IsFalse();
        await Assert.That(wallet.ExchangeQuote.UnavailableReason).IsEqualTo(reason);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    public async Task Exchange_ConcurrentRequests_CommitOneDebitAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28407);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28407, 1, 10);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => api.Player.ExchangeEnergyAsync(CancellationToken.None)));
        var stored = await GetStoredPlayerAsync(28407);
        var next = await api.Player.ExchangeEnergyAsync(CancellationToken.None);

        // Assert
        await Assert.That(results.Count(result => result.Exchanged)).IsEqualTo(1);
        await Assert.That(next.Exchanged).IsFalse();
        await Assert.That(stored["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(stored["Wallet"]["Crystals"].AsInt32).IsEqualTo(7);
    }

    private static async Task<EnergyWalletResponse> ReadWalletAsync(TestApiClient api) =>
        (await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).EnergyWallet
        ?? throw new InvalidOperationException("The player response has no wallet.");

    private Task<BsonDocument> GetStoredInviteAsync(string inviteeId) =>
        Database.GetCollection<BsonDocument>("invites").Find(new BsonDocument("InviteeId", new BsonBinaryData(Guid.Parse(inviteeId), GuidRepresentation.Standard)))
            .SingleAsync(CancellationToken.None);

    private Task SetWalletAsync(long userId, int energy, int crystals) =>
        Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            player => player.ExternalUserIds.Contains(ExternalUserId.From($"telegram:{userId}")),
            Builders<PlayerProfile>.Update
                .Set(player => player.Wallet.Energy, energy)
                .Set(player => player.Wallet.Crystals, crystals)
                .Set(player => player.Wallet.EnergyUpdatedAtUtc, Factory.Session.Clock.GetUtcNow())
                .Inc(player => player.Version, 1));
}
