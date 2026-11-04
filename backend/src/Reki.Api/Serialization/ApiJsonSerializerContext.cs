using System.Net.ServerSentEvents;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Api.Requests.Events;
using Reki.Api.Requests.Player;
using Reki.Api.Requests.Posts;
using Reki.Api.Requests.Social;
using Reki.Api.Requests.Telemetry;
using Reki.Api.Requests.Telemetry.AnalyticsBatch;
using Reki.Api.Requests.Telemetry.ClientLogBatch;
using Reki.Api.Requests.Worlds;
using Reki.Application.Events.Responses;
using Reki.Application.Player.Responses;
using Reki.Application.Posts;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.Responses.ChannelPost;
using Reki.Application.Posts.Responses.FeedItem;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.Posts.Responses.PublicationStatus;
using Reki.Application.Posts.Responses.PublicationStatusLiveEvent;
using Reki.Application.Posts.Responses.ReplyLiveEvent;
using Reki.Application.Responses;
using Reki.Application.Social.Responses;
using Reki.Application.Social.Responses.Challenge;
using Reki.Application.Social.Responses.Dm;
using Reki.Application.Telemetry;
using Reki.Application.Worlds.Responses;
using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnCommentPublished;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnFeedOpened;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(ExchangeEnergyRequest))]
[JsonSerializable(typeof(EnergyWalletResponse))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(StarAgentProfile))]
[JsonSerializable(typeof(AddressType))]
[JsonSerializable(typeof(StarFeedHistoryPost[]))]
[JsonSerializable(typeof(StarFeedSourceContent))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(StarFeedRequest))]
[JsonSerializable(typeof(StarFeedOnPostPublishedRequest))]
[JsonSerializable(typeof(StarFeedOnCommentPublishedRequest))]
[JsonSerializable(typeof(SessionId))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(TokenExchangeRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSerializable(typeof(AuthTokenResponse))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(PlayerId))]
[JsonSerializable(typeof(PostId))]
[JsonSerializable(typeof(CommentId))]
[JsonSerializable(typeof(InvitationId))]
[JsonSerializable(typeof(RekiEventId))]
[JsonSerializable(typeof(PublicationFailure))]
[JsonSerializable(typeof(Stream))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(IReadOnlyList<LiveEvent>))]
[JsonSerializable(typeof(SseItem<PublicationStatusLiveEvent>))]
[JsonSerializable(typeof(PlayerResponse))]
[JsonSerializable(typeof(ReferralResponse))]
[JsonSerializable(typeof(InvitationResponse))]
[JsonSerializable(typeof(OnboardingRequest))]
[JsonSerializable(typeof(StartPublicationRequest))]
[JsonSerializable(typeof(PublicationStatusLiveEvent))]
[JsonSerializable(typeof(PublicationStatusResponse))]
[JsonSerializable(typeof(PublicationStatusType))]
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
[JsonSerializable(typeof(WorldPostsRequest))]
[JsonSerializable(typeof(PageResponse<FeedItemResponse>))]
[JsonSerializable(typeof(PageResponse<ChannelPostResponse>))]
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
        typeof(JsonStringEnumConverter<AddressType>),
        typeof(CamelCaseEnumConverter<WorldDrama>),
        typeof(CamelCaseEnumConverter<WorldDifficulty>),
        typeof(CamelCaseEnumConverter<BirthGroup>),
        typeof(CamelCaseEnumConverter<FeedItemKind>),
        typeof(CamelCaseEnumConverter<DirectMessageAuthor>),
        typeof(CamelCaseEnumConverter<GameEventKind>),
        typeof(CamelCaseEnumConverter<LiveEventType>),
        typeof(CamelCaseEnumConverter<ReplyStatus>),
        typeof(CamelCaseEnumConverter<ClientLogLevel>),
        typeof(KebabCaseEnumConverter<LaunchSurface>),
        typeof(SnakeCaseEnumConverter<PublicationStatusType>)
    ],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ApiJsonSerializerContext : JsonSerializerContext;
