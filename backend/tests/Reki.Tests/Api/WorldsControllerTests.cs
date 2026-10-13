using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Reki.Contracts.Worlds;
using Reki.Infrastructure.Serialization;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class WorldsControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task WorldReads_SeparateInstances_IsolateContentAsync()
    {
        using var api = Factory.CreateApiClient(1022);
        var player = await CreatePlayerWithWorldAsync(api);
        var firstId = Domain.Worlds.WorldId.From(await GetWorldIdAsync(api));
        var second = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var secondId = Domain.Worlds.WorldId.From(second.Id);
        var owner = Domain.Player.PlayerId.From(player.Id);
        var store = Factory.Services.GetRequiredService<Reki.Application.Common.IGameStore>();
        var posts = new[] { firstId, secondId }.Select(worldId => new Reki.Domain.Posts.GamePost(
            Domain.Posts.PostId.New(),
            owner.ToAuthorId(),
            worldId,
            worldId.Value,
            1,
            [],
            DateTimeOffset.UnixEpoch,
            0,
            0,
            1,
            false,
            false)).ToArray();
        await Database.GetCollection<Reki.Domain.Posts.GamePost>("posts").InsertManyAsync(posts);
        var starId = Domain.Social.StarId.From("mira");
        foreach (var worldId in new[] { firstId, secondId })
        {
            var conversation = new Reki.Domain.Social.StarConversation(
                Domain.Social.StarConversationId.New(),
                owner,
                worldId,
                starId,
                [worldId.Value],
                [],
                0,
                DateTimeOffset.UnixEpoch);
            await store.SaveStarConversationAsync(conversation, CancellationToken.None);
        }

        foreach (var worldId in new[] { firstId, secondId })
        {
            var worldPosts = await store.GetPostsAsync(null, worldId, null, CancellationToken.None);
            var conversation = await store.GetStarConversationAsync(owner, starId, CancellationToken.None, worldId)
                ?? throw new InvalidOperationException("Missing conversation.");
            await Assert.That(worldPosts.Single().WorldId).IsEqualTo(worldId);
            await Assert.That(conversation.Memory.Single()).IsEqualTo(worldId.Value);
        }
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Analytics_WorldInstance_ValidatesOwnershipAsync(bool own)
    {
        using var owner = Factory.CreateApiClient(1023);
        using var other = Factory.CreateApiClient(1024);
        await CreatePlayerWithWorldAsync(owner);
        await CreatePlayerWithWorldAsync(other);
        var worldId = await GetWorldIdAsync(own ? owner : other);
        using var http = CreateAuthenticatedHttpClient(1023);
        var batch = new Reki.Contracts.Telemetry.AnalyticsBatchRequest
        {
            Session = "world-session",
            Platform = "web",
            Experiments = new Dictionary<string, string>(),
            Events =
            [
                new Reki.Contracts.Telemetry.AnalyticsEventRequest
                {
                    Name = "screen_view", WorldId = worldId, Screen = "feed",
                },
            ],
        };
        using var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(batch, ApiJsonSerializerContext.Default.AnalyticsBatchRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await http.PostAsync("/events", content, CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(own
            ? System.Net.HttpStatusCode.NoContent : System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateWorld_SubmittedDefinition_CreatesFreshInstanceAsync()
    {
        using var api = Factory.CreateApiClient(1020);
        await CreatePlayerWithWorldAsync(api);
        var definition = await CreateWorldDefinitionAsync(api);
        var star = definition.Stars[0] with { Id = "my-star", Name = "Своя звезда", Relation = 5 };
        var request = definition with
        {
            Title = "Новый мир",
            AvatarStyle = "shapes",
            Roles = ["Автор"],
            Stars = [star],
            Wants = definition.Wants.Select(want => want with { Done = true }).ToArray(),
            Achievements = definition.Achievements.Select(card => card with { Open = true }).ToArray(),
        };

        var first = await api.Worlds.CreateWorldAsync(request, CancellationToken.None);
        var second = await api.Worlds.CreateWorldAsync(request, CancellationToken.None);
        var loaded = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single(item => item.Id == first.Id);

        await Assert.That(first.Id).IsNotEqualTo(second.Id);
        await Assert.That(first.Id).IsNotEqualTo("stage");
        await Assert.That(loaded.BalanceKey).IsEqualTo("stage");
        await Assert.That(loaded.Title).IsEqualTo(request.Title);
        await Assert.That(loaded.AvatarStyle).IsEqualTo(request.AvatarStyle);
        await Assert.That(loaded.Roles.Single()).IsEqualTo("Автор");
        await Assert.That(loaded.Stars.Single().Id).IsEqualTo("my-star");
        await Assert.That(loaded.Stars.Single().Relation).IsEqualTo(0);
        await Assert.That(loaded.Subscribers).IsEqualTo(120);
        await Assert.That(loaded.Wants.Any(want => want.Done)).IsFalse();
        await Assert.That(loaded.Achievements.Any(card => card.Open)).IsFalse();
    }

    [Test]
    public async Task Onboarding_ExistingPlayer_ReturnsConflictAsync()
    {
        using var api = Factory.CreateApiClient(1021);
        var firstPlayer = await CreatePlayerWithWorldAsync(api);
        var firstId = await GetWorldIdAsync(api);
        PlayerApiException? exception = null;
        try
        {
            await api.Player.CompleteOnboardingAsync(
                new Reki.Contracts.Player.OnboardingRequest
                {
                    BirthGroup = Contracts.Player.BirthGroup.Teen,
                    Address = Contracts.Player.Address.Nick,
                },
                CancellationToken.None);
        }
        catch (PlayerApiException caught)
        {
            exception = caught;
        }

        var worlds = await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);
        var first = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single(item => item.Id == firstId);
        var current = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        await Assert.That(exception?.StatusCode).IsEqualTo(409);
        await Assert.That(current.Id).IsEqualTo(firstPlayer.Id);
        await Assert.That(worlds.Single().Id).IsEqualTo(firstId);
        await Assert.That(first.Name).IsEqualTo("Player");
    }

    [Test]
    public async Task UpdateWorld_WithoutLockedSafetyRule_RestoresRuleAsync()
    {
        using var api = Factory.CreateApiClient(1011);
        await CreatePlayerWithWorldAsync(api, Contracts.Player.WorldId.Own);
        var request = (await CreateWorldDefinitionAsync(api)) with
        {
            Title = "Мой мир",
            Description = "Безопасное описание мира",
            Rules = [new WorldRuleResponse("Звёзды любят короткие посты")],
        };

        await api.Worlds.UpdateWorldAsync(await GetWorldIdAsync(api), request, CancellationToken.None);
        var rules = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single();

        var safetyRule = rules.Rules.Single(rule => rule.Locked);
        await Assert.That(safetyRule.Text).IsEqualTo("Без тем 18+ и травли по внешности");
    }

    [Test]
    public async Task GetWorldTemplates_Anonymous_ReturnsCompleteTemplatesAsync()
    {
        using var api = Factory.CreateApiClient();
        var templates = await api.Worlds.GetWorldTemplatesAsync(CancellationToken.None);
        var template = templates.Single(world => world.BalanceKey == "evening");
        await Assert.That(template.Stars.Count).IsGreaterThan(0);
        await Assert.That(template.Roles.Count).IsGreaterThan(0);
        var safetyRule = template.Rules.Single(rule => rule.Text == "Без тем 18+ и травли по внешности");
        await Assert.That(safetyRule.Locked).IsTrue();
        using var http = Factory.CreateClient();
        using var owned = await http.GetAsync("/worlds", CancellationToken.None);
        await Assert.That(owned.StatusCode).IsEqualTo(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetOwnedWorlds_TwoWorlds_ReturnsCompleteInstancesAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(1015);
        var player = await CreatePlayerWithWorldAsync(api);
        var firstId = await GetWorldIdAsync(api);
        var request = (await CreateWorldDefinitionAsync(api, Contracts.Player.WorldId.Academy)) with
        {
            Name = "Второй канал",
            Avatar = "second",
            Role = "Студент",
        };
        var second = await api.Worlds.CreateWorldAsync(request, CancellationToken.None);

        // Act
        var worlds = await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);
        var first = worlds.Single(item => item.Id == firstId);
        var selected = worlds.Single(item => item.Id == second.Id);
        var currentPlayer = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);

        // Assert
        await Assert.That(first.Id).IsNotEqualTo(selected.Id);
        await Assert.That(first.BalanceKey).IsEqualTo("stage");
        await Assert.That(selected.BalanceKey).IsEqualTo("academy");
        await Assert.That(selected.Name).IsEqualTo("Второй канал");
        await Assert.That(selected.Stars.Any(star => star.Id == "aiko")).IsTrue();
        await Assert.That(currentPlayer.Id).IsEqualTo(player.Id);
    }

    [Test]
    public async Task GetOwnedWorlds_OtherOwner_ExcludesForeignWorldAsync()
    {
        // Arrange
        using var owner = Factory.CreateApiClient(1016);
        using var other = Factory.CreateApiClient(1017);
        await CreatePlayerWithWorldAsync(owner);
        await CreatePlayerWithWorldAsync(other);
        var id = await GetWorldIdAsync(owner);

        // Act
        var worlds = await other.Worlds.GetOwnedWorldsAsync(CancellationToken.None);

        // Assert
        await Assert.That(worlds.Count).IsEqualTo(1);
        await Assert.That(worlds.Any(world => world.Id == id)).IsFalse();
    }

    [Test]
    public async Task GetFeed_MissingWorldId_ReturnsBadRequestAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(1018);
        await CreatePlayerWithWorldAsync(api);
        using var http = CreateAuthenticatedHttpClient(1018);

        // Act
        using var response = await http.GetAsync("/feed", CancellationToken.None);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetPlayer_DoesNotIncludeWorldAttributesAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(1019);
        await CreatePlayerWithWorldAsync(api);
        using var http = CreateAuthenticatedHttpClient(1019);

        // Act
        using var response = await http.GetAsync("/me", CancellationToken.None);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken.None));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(body.RootElement.TryGetProperty("world", out _)).IsFalse();
        await Assert.That(body.RootElement.TryGetProperty("subs", out _)).IsFalse();
        await Assert.That(body.RootElement.GetProperty("settings").TryGetProperty("drama", out _)).IsFalse();
    }

    [Test]
    public async Task CurrentPresets_FullUpdate_PreservesProgressAsync()
    {
        using var api = Factory.CreateApiClient(1091);
        await api.Player.CompleteOnboardingAsync(
            new Contracts.Player.OnboardingRequest
        {
            BirthGroup = Contracts.Player.BirthGroup.Teen,
            Address = Contracts.Player.Address.Nick,
        },
            CancellationToken.None);
        var templates = await api.Worlds.GetWorldTemplatesAsync(CancellationToken.None);
        await Assert.That(templates.Select(template => template.BalanceKey)).IsEquivalentTo(["it", "evening", "sorcerers", "houses"]);
        foreach (var template in templates)
        {
            var definition = DefinitionOf(template);
            var created = await api.Worlds.CreateWorldAsync(definition, CancellationToken.None);
            var collection = Database.GetCollection<Domain.Worlds.World>("worlds");
            var id = Domain.Worlds.WorldId.From(created.Id);
            var original = await collection.Find(world => world.Id == id).SingleAsync();
            var progressed = original with
            {
                Stars = original.Stars.Select(star => star with { Relation = 3 }).ToArray(),
                Player = original.Player with
                {
                    Subscribers = 730,
                    Wants = original.Player.Wants.Select(want => want with { Done = true }).ToArray(),
                    Achievements = original.Player.Achievements.Select(item => item with { IsOpen = true }).ToArray(),
                },
            };
            await collection.ReplaceOneAsync(world => world.Id == id, progressed);
            var removed = progressed.Stars[0];
            var request = definition with
            {
                Title = "Изменённый мир",
                BalanceKey = "own",
                Stars = definition.Stars.Skip(1).ToArray(),
                Roles = ["Хроникёр"],
                PostIdeas = ["Новая идея"],
            };
            await api.Worlds.UpdateWorldAsync(created.Id, request, CancellationToken.None);
            var updated = await collection.Find(world => world.Id == id).SingleAsync();
            await Assert.That(updated.BalanceKey).IsEqualTo(original.BalanceKey);
            await Assert.That(updated.Player.PlayerId).IsEqualTo(original.Player.PlayerId);
            await Assert.That(updated.Player.Subscribers).IsEqualTo(730L);
            await Assert.That(updated.Stars.All(star => star.Relation == 3)).IsTrue();
            await Assert.That(updated.Player.Wants.All(want => want.Done)).IsTrue();
            await Assert.That(updated.Player.Achievements.All(item => item.IsOpen)).IsTrue();
            await Assert.That(updated.FindStar(removed.Id.ToAuthorId())?.Name).IsEqualTo(removed.Name);
            await Assert.That(updated.Interface.Title).IsEqualTo(request.Title);
            await Assert.That(updated.PostIdeas).IsEquivalentTo(request.PostIdeas);
        }
    }

    [Test]
    public async Task OwnWorldCreation_RequiresPermanentMilestoneAsync()
    {
        using var api = Factory.CreateApiClient(1092);
        var player = await CreatePlayerWithWorldAsync(api);
        var definition = await CreateWorldDefinitionAsync(api, Contracts.Player.WorldId.Own);
        WorldsApiException? rejected = null;
        try
        {
            await api.Worlds.CreateWorldAsync(definition, CancellationToken.None);
        }
        catch (WorldsApiException exception)
        {
            rejected = exception;
        }

        await Assert.That(rejected?.StatusCode).IsEqualTo(409);
        await Assert.That((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Count).IsEqualTo(1);
        await Database.GetCollection<Domain.Player.PlayerProfile>("players").UpdateOneAsync(
            item => item.Id == Domain.Player.PlayerId.From(player.Id),
            Builders<Domain.Player.PlayerProfile>.Update.Set(item => item.HitOwnWorldMilestone, true));
        var created = await api.Worlds.CreateWorldAsync(definition, CancellationToken.None);
        await Assert.That(created.BalanceKey).IsEqualTo("own");
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).HitOwnWorldMilestone).IsTrue();
    }

    [Test]
    public async Task Publication_CrossesThreshold_PermanentlyUnlocksOwnWorldAsync()
    {
        using var api = Factory.CreateApiClient(1093);
        await CreatePlayerWithWorldAsync(api);
        var id = Domain.Worlds.WorldId.From(await GetWorldIdAsync(api));
        await Database.GetCollection<Domain.Worlds.World>("worlds").UpdateOneAsync(
            world => world.Id == id,
            Builders<Domain.Worlds.World>.Update.Set(world => world.Player.Subscribers, 499));
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).HitOwnWorldMilestone).IsFalse();

        // Fixed publication seed produces a positive turn; flops must not unlock the milestone.
        await PublishWithIdAsync(1093, "Расскажу о сегодняшней репетиции", "00000000-0000-0000-0000-000000000001");
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).HitOwnWorldMilestone).IsTrue();
        await Database.GetCollection<Domain.Worlds.World>("worlds").UpdateOneAsync(
            world => world.Id == id,
            Builders<Domain.Worlds.World>.Update.Set(world => world.Player.Subscribers, 100));
        await Assert.That((await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).HitOwnWorldMilestone).IsTrue();
    }
}