using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(PlayerResponse))]
[JsonSerializable(typeof(OnboardingRequest))]
[JsonSerializable(typeof(CreatePostRequest))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(DirectMessageRequest))]
[JsonSerializable(typeof(ForgetMemoryRequest))]
[JsonSerializable(typeof(AnswerEventRequest))]
[JsonSerializable(typeof(StartChallengeRequest))]
[JsonSerializable(typeof(ReportRequest))]
[JsonSerializable(typeof(UpdateSettingsRequest))]
[JsonSerializable(typeof(FeedItemResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<FeedItemResponse>))]
[JsonSerializable(typeof(FriendResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<FriendResponse>))]
[JsonSerializable(typeof(DmResponse))]
[JsonSerializable(typeof(SendDirectMessageResponse))]
[JsonSerializable(typeof(ChallengeResponse))]
[JsonSerializable(typeof(GameEventResponse))]
[JsonSerializable(typeof(WorldRulesResponse))]
[JsonSerializable(typeof(UpdateWorldRulesRequest))]
[JsonSerializable(typeof(PlusInterestResponse))]
[JsonSerializable(typeof(ViewsLiveEvent))]
[JsonSerializable(typeof(ReactionLiveEvent))]
[JsonSerializable(typeof(CommentLiveEvent))]
[JsonSerializable(typeof(TypingLiveEvent))]
[JsonSerializable(typeof(ResultLiveEvent))]
[JsonSerializable(typeof(AnalyticsBatchRequest))]
[JsonSerializable(typeof(ClientLogBatchRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class ApiJsonSerializerContext : JsonSerializerContext;
