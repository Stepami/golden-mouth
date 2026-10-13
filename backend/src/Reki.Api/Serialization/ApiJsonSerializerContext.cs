using System.Net.ServerSentEvents;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(Stream))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IReadOnlyList<LiveEvent>))]
[JsonSerializable(typeof(SseItem<PublicationStatusLiveEvent>))]
[JsonSerializable(typeof(PublicationFailure))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(PlayerResponse))]
[JsonSerializable(typeof(ReferralResponse))]
[JsonSerializable(typeof(OnboardingRequest))]
[JsonSerializable(typeof(StartPublicationRequest))]
[JsonSerializable(typeof(PublicationStatusLiveEvent))]
[JsonSerializable(typeof(PublicationStatusResponse))]
[JsonSerializable(typeof(PublicationStatus))]
[JsonSerializable(typeof(LiveEventType))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(ReplyStatus))]
[JsonSerializable(typeof(ReplyLiveEvent))]
[JsonSerializable(typeof(SseItem<ReplyLiveEvent>))]
[JsonSerializable(typeof(DirectMessageRequest))]
[JsonSerializable(typeof(ForgetMemoryRequest))]
[JsonSerializable(typeof(AnswerEventRequest))]
[JsonSerializable(typeof(StartChallengeRequest))]
[JsonSerializable(typeof(ReportRequest))]
[JsonSerializable(typeof(UpdateSettingsRequest))]
[JsonSerializable(typeof(FeedItemResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<FeedItemResponse>))]
[JsonSerializable(typeof(ChannelPostResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<ChannelPostResponse>))]
[JsonSerializable(typeof(PostDetailResponse))]
[JsonSerializable(typeof(FriendResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<FriendResponse>))]
[JsonSerializable(typeof(DmResponse))]
[JsonSerializable(typeof(SendDirectMessageResponse))]
[JsonSerializable(typeof(ChallengeResponse))]
[JsonSerializable(typeof(GameEventResponse))]
[JsonSerializable(typeof(WorldResponse))]
[JsonSerializable(typeof(IReadOnlyList<WorldResponse>))]
[JsonSerializable(typeof(CreateWorldRequest))]
[JsonSerializable(typeof(PlusInterestRequest))]
[JsonSerializable(typeof(PlusInterestResponse))]
[JsonSerializable(typeof(ViewsLiveEvent))]
[JsonSerializable(typeof(LiveEvent))]
[JsonSerializable(typeof(ReactionLiveEvent))]
[JsonSerializable(typeof(CommentLiveEvent))]
[JsonSerializable(typeof(TypingLiveEvent))]
[JsonSerializable(typeof(ResultLiveEvent))]
[JsonSerializable(typeof(AnalyticsBatchRequest))]
[JsonSerializable(typeof(ClientLogBatchRequest))]
[JsonSourceGenerationOptions(
    Converters =
    [
        typeof(CamelCaseEnumConverter<SkillId>),
        typeof(CamelCaseEnumConverter<Address>),
        typeof(CamelCaseEnumConverter<Drama>),
        typeof(CamelCaseEnumConverter<BirthGroup>),
        typeof(CamelCaseEnumConverter<FeedItemKind>),
        typeof(CamelCaseEnumConverter<DirectMessageAuthor>),
        typeof(CamelCaseEnumConverter<GameEventKind>),
        typeof(CamelCaseEnumConverter<LiveEventType>),
        typeof(CamelCaseEnumConverter<ReplyStatus>),
        typeof(CamelCaseEnumConverter<ClientLogLevel>),
        typeof(KebabCaseEnumConverter<LaunchSurface>),
        typeof(SnakeCaseEnumConverter<PublicationStatus>)
    ],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class ApiJsonSerializerContext : JsonSerializerContext;