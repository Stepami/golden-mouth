using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Serialization;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class ReplyStreamTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    [Arguments(false, false)]
    [Arguments(true, true)]
    [Arguments(true, false)]
    public async Task AddReply_ValidTurn_PersistsCanonicalTreeAsync(bool optional, bool reaction)
    {
        // Arrange
        const long user = 8701;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var world = await Database.GetCollection<World>("worlds").Find(FilterDefinition<World>.Empty).SingleAsync();
        var original = await GetStoredPostAsync(postId);
        var selected = original["Comments"][0]["AuthorId"]["Id"].AsString;
        var other = world.Stars.First(star => star.Id.Value != selected);
        Factory.Llm.Respond(CreateReplyTurnResponse(selected, args =>
        {
            if (optional)
            {
                args["emoji"] = reaction ? "💡" : string.Empty;
                args["additional_star_id"] = other.Id.Value;
                args["additional_text"] = "Есть ещё один взгляд на тему.";
                args["additional_tone"] = "critique";
            }
        }));

        // Act
        var response = await SendReplyAsync(user, postId, targetId, "valid-turn", "Ответ на мысль звезды");
        var events = ParseReplyEvents(response.Body);
        var completed = events[^1];
        var stored = await GetStoredPostAsync(postId);
        using var api = Factory.CreateApiClient(user);
        var detail = await api.Posts.GetPostAsync(postId, world.Id.Value, CancellationToken.None);

        // Assert
        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.MediaType).IsEqualTo("text/event-stream");
        await Assert.That(events[0].Status).IsEqualTo("generating");
        await Assert.That(events.Any(item => item.Status == "generating" && item.Text == "Спасибо за ответ. Продолжим обсуждение.")).IsTrue();
        await Assert.That(completed.Status).IsEqualTo("completed");
        await Assert.That(completed.RequestId).IsEqualTo("valid-turn");
        await Assert.That((completed.Post ?? throw new InvalidOperationException("Missing completed post.")).Comments.Count).IsEqualTo(optional ? 4 : 3);
        await Assert.That(detail.Comments.Select(comment => comment.Id)).IsEquivalentTo((completed.Post ?? throw new InvalidOperationException("Missing completed post.")).Comments.Select(comment => comment.Id));
        var playerReply = detail.Comments.Single(comment => comment.RequestId == "valid-turn");
        var starReply = detail.Comments.Single(comment => comment.ParentCommentId == playerReply.Id);
        await Assert.That(playerReply.ParentCommentId).IsEqualTo(targetId);
        await Assert.That(playerReply.Me).IsTrue();
        await Assert.That(playerReply.Star).IsFalse();
        await Assert.That(playerReply.Depth).IsEqualTo(1);
        await Assert.That(starReply.AuthorId).IsEqualTo(selected);
        await Assert.That(starReply.RequestId).IsNull();
        await Assert.That(starReply.Me).IsFalse();
        await Assert.That(starReply.Depth).IsEqualTo(2);
        await Assert.That(playerReply.Reactions.Count).IsEqualTo(reaction ? 1 : 0);
        if (optional)
        {
            if (reaction)
            {
                await Assert.That(playerReply.Reactions.Single().Emoji).IsEqualTo("💡");
                await Assert.That(playerReply.Reactions.Single().AuthorId).IsEqualTo(selected);
            }

            var additional = detail.Comments.Single(comment => comment.AuthorId == other.Id.Value);
            await Assert.That(additional.ParentCommentId).IsNull();
            await Assert.That(additional.Depth).IsEqualTo(0);
            await Assert.That(additional.RequestId).IsNull();
            await Assert.That(additional.Text).IsEqualTo("Есть ещё один взгляд на тему.");
            await Assert.That((completed.Post ?? throw new InvalidOperationException("Missing completed post.")).Comments.Single(comment => comment.Id == additional.Id).Text).IsEqualTo("Есть ещё один взгляд на тему.");
            await Assert.That(stored["Comments"].AsBsonArray.Single(comment => comment["_id"].AsString == additional.Id)["Text"].AsString).IsEqualTo("Есть ещё один взгляд на тему.");
        }

        var player = await GetStoredPlayerAsync(user);
        await Assert.That(player["EnergyUsed"].AsInt32).IsEqualTo(beforePlayer["EnergyUsed"].AsInt32 + 1);
        await Assert.That(completed.EnergyLeft).IsEqualTo(8);
        var storedOwn = stored["Comments"].AsBsonArray.Single(item => item["RequestId"].IsString && item["RequestId"].AsString == "valid-turn");
        await Assert.That(storedOwn["AuthorId"]["Kind"].AsInt32).IsEqualTo(0);
        await Assert.That(storedOwn["AuthorId"]["Id"]).IsEqualTo(beforePlayer["_id"]);
        var storedStar = stored["Comments"].AsBsonArray.Single(item => item["ParentCommentId"].IsString && item["ParentCommentId"].AsString == storedOwn["_id"].AsString);
        await Assert.That(storedStar["AuthorId"]["Kind"].AsInt32).IsEqualTo(1);
        await Assert.That(storedStar["AuthorId"]["Id"].AsString).IsEqualTo(selected);
        await Assert.That(stored["Version"].AsInt32).IsEqualTo(1);
        await Assert.That(stored.Contains("ThreadReplies")).IsFalse();
        foreach (var comment in stored["Comments"].AsBsonArray.Select(value => value.AsBsonDocument))
        {
            await Assert.That(comment["AuthorId"].AsBsonDocument.Contains("Id")).IsTrue();
            await Assert.That(comment.Contains("EnergySpent") || comment.Contains("SubscriberBonus") || comment.Contains("Me") || comment.Contains("Star") || comment.Contains("Author") || comment.Contains("Avatar") || comment.Contains("Handle") || comment.Contains("OriginCommentId")).IsFalse();
        }
    }

    [Test]
    [Arguments("text")]
    [Arguments("target")]
    public async Task AddReply_SavedKeyChangedPayload_ConflictsWithoutMutationAsync(string changed)
    {
        // Arrange
        const long user = 8702;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var first = await ReplyAsync(user, postId, targetId, "saved-key", "исходный ответ");
        var post = await Database.GetCollection<GamePost>("posts").Find(item => item.Id == PostId.From(postId)).SingleAsync();
        var secondRoot = post.Comments[0] with { Id = CommentId.New(), Text = "другая ветка" };
        await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, post with { Comments = [.. post.Comments, secondRoot] });
        var before = await GetStoredPostAsync(postId);
        var player = await GetStoredPlayerAsync(user);
        var calls = Factory.Llm.RequestCount;

        // Act
        var response = await SendReplyAsync(user, postId, changed == "target" ? secondRoot.Id.Value : targetId, "saved-key", changed == "text" ? "другой ответ" : "исходный ответ");

        // Assert
        await Assert.That(response.Status).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(before);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"]).IsEqualTo(player["EnergyUsed"]);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(calls);
        await Assert.That((first.Post ?? throw new InvalidOperationException("Missing first post.")).Comments.Count).IsEqualTo(3);
    }

    [Test]
    public async Task AddReply_SavedRequest_ReturnsCurrentPostWithoutModelOrChargeAsync()
    {
        // Arrange
        const long user = 8703;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var first = await ReplyAsync(user, postId, targetId, "recover", "сохранённый ответ");
        await Database.GetCollection<BsonDocument>("posts").UpdateOneAsync(new BsonDocument("_id", postId), new BsonDocument("$set", new BsonDocument("Views", 999)));
        var calls = Factory.Llm.RequestCount;
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeWorld = await GetStoredWorldAsync(user);

        // Act
        var retry = await ReplyAsync(user, postId, targetId, "recover", "сохранённый ответ");

        // Assert
        await Assert.That((retry.Post ?? throw new InvalidOperationException("Missing retry post.")).Views).IsEqualTo(999);
        await Assert.That((retry.Post ?? throw new InvalidOperationException("Missing retry post.")).Comments.Select(comment => comment.Id)).IsEquivalentTo((first.Post ?? throw new InvalidOperationException("Missing first post.")).Comments.Select(comment => comment.Id));
        await Assert.That(retry.EnergyLeft).IsEqualTo(first.EnergyLeft);
        await Assert.That(retry.Subscribers).IsEqualTo(first.Subscribers);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"]).IsEqualTo(beforePlayer["EnergyUsed"]);
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"]).IsEqualTo(beforeWorld["Player"]["Subscribers"]);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(calls);
    }

    [Test]
    [Arguments("wrong-star")]
    [Arguments("missing-key")]
    [Arguments("accounting")]
    [Arguments("unsafe")]
    [Arguments("foreign-extra")]
    [Arguments("same-extra")]
    [Arguments("partial-extra")]
    [Arguments("hate")]
    [Arguments("long-text")]
    [Arguments("long-emoji")]
    [Arguments("non-string")]
    [Arguments("wrong-tool")]
    [Arguments("no-tool")]
    public async Task AddReply_InvalidModelTurn_FailsWithoutPublishingOrChargingAsync(string violation)
    {
        // Arrange
        const long user = 8704;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var before = await GetStoredPostAsync(postId);
        var player = await GetStoredPlayerAsync(user);
        var world = await GetStoredWorldAsync(user);
        var selected = before["Comments"][0]["AuthorId"]["Id"].AsString;
        var response = CreateReplyTurnResponse(selected, args =>
        {
            switch (violation)
            {
                case "wrong-star":
                {
                    args["star_id"] = "outside-world";
                    break;
                }

                case "missing-key":
                {
                    args.Remove("emoji");
                    break;
                }

                case "accounting":
                {
                    args["energy_spent"] = 0;
                    break;
                }

                case "unsafe":
                {
                    args["text"] = "наркотики";
                    break;
                }

                case "foreign-extra":
                {
                    args["additional_star_id"] = "outside-world";
                    args["additional_text"] = "ответ";
                    args["additional_tone"] = "support";
                    break;
                }

                case "same-extra":
                {
                    args["additional_star_id"] = selected;
                    args["additional_text"] = "ответ";
                    args["additional_tone"] = "support";
                    break;
                }

                case "partial-extra":
                {
                    args["additional_text"] = "ответ без автора";
                    break;
                }

                case "hate":
                {
                    args["tone"] = "hate";
                    break;
                }

                case "long-text":
                {
                    args["text"] = new string('а', 501);
                    break;
                }

                case "long-emoji":
                {
                    args["emoji"] = new string('а', 9);
                    break;
                }

                case "non-string":
                {
                    args["text"] = 42;
                    break;
                }
            }
        });
        if (violation == "wrong-tool")
        {
            var arguments = response.Messages[0].Contents.OfType<FunctionCallContent>().Single().Arguments;
            response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("wrong", "comment", arguments)]));
        }

        if (violation == "no-tool")
        {
            response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "без вызова инструмента"));
        }

        Factory.Llm.Respond(response);

        // Act
        var result = await SendReplyAsync(user, postId, targetId, "invalid-model", "безопасный ответ");
        var terminal = ParseReplyEvents(result.Body)[^1];

        // Assert
        await Assert.That(terminal.Status).IsEqualTo("failed");
        await Assert.That(terminal.Code).IsEqualTo("ai_unavailable");
        await Assert.That(terminal.Post).IsNull();
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(before);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"]).IsEqualTo(player["EnergyUsed"]);
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"]).IsEqualTo(world["Player"]["Subscribers"]);
    }

    [Test]
    [Arguments("blank")]
    [Arguments("long")]
    [Arguments("key")]
    [Arguments("unsafe")]
    [Arguments("missing-target")]
    [Arguments("missing-post")]
    [Arguments("missing-world")]
    [Arguments("player-target")]
    [Arguments("unknown-star")]
    [Arguments("depth")]
    [Arguments("energy")]
    [Arguments("energy-over")]
    [Arguments("depth-adjacent")]
    public async Task AddReply_InvalidInput_RejectsBeforeGenerationAsync(string scenario)
    {
        // Arrange
        const long user = 8705;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var text = scenario switch { "blank" => " ", "long" => new string('а', 1001), "unsafe" => "наркотики", _ => "ответ" };
        var key = scenario == "key" ? string.Empty : "input-validation";
        if (scenario is "player-target" or "unknown-star")
        {
            var author = scenario == "player-target"
                ? new BsonDocument { ["Kind"] = 0, ["Id"] = (await GetStoredPlayerAsync(user))["_id"] }
                : new BsonDocument { ["Kind"] = 1, ["Id"] = "unknown-star" };
            await Database.GetCollection<BsonDocument>("posts").UpdateOneAsync(new BsonDocument("_id", postId), new BsonDocument("$set", new BsonDocument("Comments.0.AuthorId", author)));
        }

        if (scenario == "depth")
        {
            var reply = await ReplyAsync(user, postId, targetId, "depth-seed", "первый ответ");
            targetId = (reply.Post ?? throw new InvalidOperationException("Missing reply post.")).Comments.Single(comment => comment.Depth == 2).Id;
        }

        if (scenario == "depth-adjacent")
        {
            var posts = Database.GetCollection<GamePost>("posts");
            var post = await posts.Find(item => item.Id == PostId.From(postId)).SingleAsync();
            var nested = post.Comments[0] with { Id = CommentId.New(), ParentCommentId = post.Comments[0].Id };
            await posts.ReplaceOneAsync(item => item.Id == post.Id, post with { Comments = [.. post.Comments, nested] });
            targetId = nested.Id.Value;
        }

        if (scenario is "energy" or "energy-over")
        {
            var playerId = (await GetStoredPlayerAsync(user))["_id"];
            await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", playerId), new BsonDocument("$set", new BsonDocument("EnergyUsed", scenario == "energy" ? 10 : 11)));
        }

        var before = await GetStoredPostAsync(postId);
        var player = await GetStoredPlayerAsync(user);
        var world = await GetStoredWorldAsync(user);
        var calls = Factory.Llm.RequestCount;

        // Act
        var result = await SendReplyAsync(user, scenario == "missing-post" ? "missing-post" : postId, scenario == "missing-target" ? "missing-target" : targetId, key, text, scenario == "missing-world" ? "missing-world" : string.Empty);

        // Assert
        var expectedStatus = scenario switch
        {
            "missing-post" or "missing-target" or "missing-world" => HttpStatusCode.NotFound,
            "unsafe" => HttpStatusCode.UnprocessableEntity,
            "blank" or "long" or "key" => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.Conflict,
        };
        await Assert.That(result.Status).IsEqualTo(expectedStatus);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(before);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"]).IsEqualTo(player["EnergyUsed"]);
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"]).IsEqualTo(world["Player"]["Subscribers"]);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(calls);
    }

    [Test]
    public async Task AddReply_ExactTextLimit_CommitsOnceAsync()
    {
        // Arrange
        const long user = 8706;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var playerId = (await GetStoredPlayerAsync(user))["_id"];
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", playerId), new BsonDocument("$set", new BsonDocument("EnergyUsed", 9)));

        // Act
        var completed = await ReplyAsync(user, postId, targetId, "exact-limit", new string('а', 1000));
        var replay = await ReplyAsync(user, postId, targetId, "exact-limit", new string('а', 1000));

        // Assert
        await Assert.That(completed.EnergyLeft).IsEqualTo(0);
        await Assert.That(replay.EnergyLeft).IsEqualTo(0);
        await Assert.That((replay.Post ?? throw new InvalidOperationException("Missing replay post.")).Comments.Count).IsEqualTo(3);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(10);
    }

    [Test]
    public async Task AddReply_HistoricalUnkeyedReplies_ConsumeExistingBonusSlotsAsync()
    {
        // Arrange
        const long user = 8707;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var player = await GetStoredPlayerAsync(user);
        var post = await Database.GetCollection<GamePost>("posts").Find(item => item.Id == PostId.From(postId)).SingleAsync();
        var historical = Enumerable.Range(0, 3).Select(index => new GameComment(CommentId.New(), CommentId.From(targetId), null, PlayerId.From(player["_id"].AsString).ToAuthorId(), $"исторический ответ {index}", false, [])).ToArray();
        await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, post with { Comments = [.. post.Comments, .. historical] });
        var before = await GetStoredWorldAsync(user);

        // Act
        var completed = await ReplyAsync(user, postId, targetId, "after-history", "новый ответ");

        // Assert
        await Assert.That(completed.Subscribers).IsEqualTo(before["Player"]["Subscribers"].ToInt64());
        await Assert.That((completed.Post ?? throw new InvalidOperationException("Missing completed post.")).Comments.Count).IsEqualTo(6);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(2);
    }

    [Test]
    public async Task GetPost_CurrentWorldMetadata_ResolvesCurrentAuthorsAsync()
    {
        // Arrange
        const long user = 8708;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var selected = (await GetStoredPostAsync(postId))["Comments"][0]["AuthorId"]["Id"].AsString;
        Factory.Llm.Respond(CreateReplyTurnResponse(selected, args => args["emoji"] = "💡"));
        await ReplyAsync(user, postId, targetId, "metadata", "мой ответ");
        var worlds = Database.GetCollection<World>("worlds");
        var world = await worlds.Find(FilterDefinition<World>.Empty).SingleAsync();
        var updated = world with
        {
            Player = world.Player with { Name = "Новое имя", Avatar = "new-avatar", Role = "new-role" },
            Stars = world.Stars.Select(star => star.Id.Value == selected ? star with { Name = "Новое имя звезды", AvatarSeed = "star-avatar", Role = "star-role" } : star).ToArray(),
        };
        await worlds.ReplaceOneAsync(item => item.Id == world.Id, updated);
        var posts = Database.GetCollection<GamePost>("posts");
        var post = await posts.Find(item => item.Id == PostId.From(postId)).SingleAsync();
        await posts.ReplaceOneAsync(item => item.Id == post.Id, post with { AuthorId = StarId.From(selected).ToAuthorId() });
        using var api = Factory.CreateApiClient(user);

        // Act
        var detail = await api.Posts.GetPostAsync(postId, world.Id.Value, CancellationToken.None);
        var feed = await api.Posts.GetFeedAsync(world.Id.Value, CancellationToken.None);
        var turn = await ReplyAsync(user, postId, targetId, "star-post", "ответ на пост звезды");

        // Assert
        var own = detail.Comments.Single(comment => comment.Me);
        await Assert.That(own.Author).IsEqualTo("Новое имя");
        await Assert.That(own.Avatar).IsEqualTo("new-avatar");
        await Assert.That(own.Role).IsEqualTo("new-role");
        await Assert.That(own.Reactions.Single().Author).IsEqualTo("Новое имя звезды");
        await Assert.That(own.Reactions.Single().Avatar).IsEqualTo("star-avatar");
        await Assert.That(detail.Comments[0].Author).IsEqualTo("Новое имя звезды");
        await Assert.That(feed.Single().Author).IsEqualTo("Новое имя звезды");
        await Assert.That(feed.Single().Role).IsEqualTo("star-role");
        await Assert.That(detail.CanReply).IsTrue();
        await Assert.That(turn.Status).IsEqualTo("completed");
    }

    [Test]
    public async Task AddReply_SelectedThread_IncludesSelectedContextAsync()
    {
        // Arrange
        const long user = 8709;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var posts = Database.GetCollection<GamePost>("posts");
        var post = await posts.Find(item => item.Id == PostId.From(postId)).SingleAsync();
        await posts.ReplaceOneAsync(item => item.Id == post.Id, post with
        {
            Comments = [.. post.Comments, post.Comments[0] with { Id = CommentId.New(), Text = "unrelated-sibling-marker" }],
        });

        // Act
        await ReplyAsync(user, postId, targetId, "prompt", "player-response-marker");
        var messages = GetCapturedChatMessages();
        var prompt = string.Join("\n", messages.Select(message => message.Text));

        // Assert
        await Assert.That(prompt).Contains("пост для треда");
        await Assert.That(prompt).Contains(post.Comments[0].Text);
        await Assert.That(prompt).Contains("player-response-marker");
        await Assert.That(prompt).Contains("thread_reply");
        await Assert.That(prompt).Contains("DATA_JSON:");
        await Assert.That(prompt.Contains("unrelated-sibling-marker", StringComparison.Ordinal)).IsFalse();
        await Assert.That(prompt.Contains("любит короткие посты", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task AddReply_TwoOwnedWorlds_ConsumesSharedEnergyAsync()
    {
        // Arrange
        const long user = 8710;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        await ReplyAsync(user, postId, targetId, "first-world", "ответ в первом мире");
        using var api = Factory.CreateApiClient(user);
        var second = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var posts = Database.GetCollection<GamePost>("posts");
        var original = await posts.Find(item => item.Id == PostId.From(postId)).SingleAsync();
        var secondPost = original with
        {
            Id = PostId.New(),
            WorldId = WorldId.From(second.Id),
            Comments = [original.Comments[0]],
            Version = 0,
        };
        await posts.InsertOneAsync(secondPost);

        // Act
        var response = await SendReplyAsync(user, secondPost.Id.Value, targetId, "second-world", "ответ во втором мире", second.Id);
        var completed = ParseReplyEvents(response.Body)[^1];

        // Assert
        await Assert.That(completed.Status).IsEqualTo("completed");
        await Assert.That(completed.EnergyLeft).IsEqualTo(7);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(3);
        await Assert.That((await GetStoredPostAsync(postId))["Comments"].AsBsonArray.Count).IsEqualTo(3);
        await Assert.That((await GetStoredPostAsync(secondPost.Id.Value))["Comments"].AsBsonArray.Count).IsEqualTo(3);
    }

    [Test]
    public async Task AddReply_NewGameDay_ResetsConsumedEnergyAsync()
    {
        // Arrange
        const long user = 8711;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var playerId = (await GetStoredPlayerAsync(user))["_id"];
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(new BsonDocument("_id", playerId), new BsonDocument("$set", new BsonDocument("EnergyUsed", 10)));
        Factory.Session.Clock.Advance(TimeSpan.FromDays(1));

        // Act
        var completed = await ReplyAsync(user, postId, targetId, "new-day", "ответ нового дня");

        // Assert
        await Assert.That(completed.EnergyLeft).IsEqualTo(9);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(1);
        await Assert.That((await GetStoredPostAsync(postId))["Comments"].AsBsonArray.Count).IsEqualTo(3);
    }

    [Test]
    public async Task AddReply_ExactModelLimits_PreservesModelOutputAsync()
    {
        // Arrange
        const long user = 8712;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var selected = (await GetStoredPostAsync(postId))["Comments"][0]["AuthorId"]["Id"].AsString;
        var text = new string('а', 500);
        const string emoji = "💡💡💡💡";
        Factory.Llm.Respond(CreateReplyTurnResponse(selected, args =>
        {
            args["text"] = text;
            args["emoji"] = emoji;
        }));

        // Act
        var completed = await ReplyAsync(user, postId, targetId, "model-boundary", "ответ");
        var post = completed.Post ?? throw new InvalidOperationException("Missing completed post.");
        var own = post.Comments.Single(comment => comment.Me);

        // Assert
        await Assert.That(post.Comments.Single(comment => comment.ParentCommentId == own.Id).Text).IsEqualTo(text);
        await Assert.That(own.Reactions.Single().Emoji).IsEqualTo(emoji);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(2);
    }

    private async Task<(HttpStatusCode Status, string Body, string? MediaType)> SendReplyAsync(
        long user, string postId, string commentId, string requestId, string text, string worldId = "")
    {
        using var http = CreateAuthenticatedHttpClient(user);
        var path = worldId.Length == 0
            ? await WorldPathAsync(http, $"/posts/{postId}/replies")
            : $"/posts/{postId}/replies?worldId={worldId}";
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        var payload = new ReplyRequest
        {
            RequestId = requestId,
            CommentId = commentId,
            Text = text,
        };
        var json = JsonSerializer.Serialize(payload, ApiJsonSerializerContext.Default.ReplyRequest);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(CancellationToken.None), response.Content.Headers.ContentType?.MediaType);
    }
}
