using System.Net.ServerSentEvents;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Auth;
using Reki.Api.Requests.Player;
using Reki.Api.Requests.Posts;
using Reki.Api.Requests.Social;
using Reki.Api.Requests.Telemetry;
using Reki.Api.Requests.Telemetry.AnalyticsBatch;
using Reki.Api.Requests.Telemetry.ClientLogBatch;
using Reki.Api.Requests.Worlds;
using Reki.Application.Invites.Responses;
using Reki.Application.Player.Responses;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.FeedItem;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.Responses;
using Reki.Application.Social.Responses;
using Reki.Application.Social.Responses.Challenge;
using Reki.Application.Social.Responses.Dm;
using Reki.Application.Telemetry;
using Reki.Application.Worlds.Responses;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Player.Streaks;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnCommentPublished;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnFeedOpened;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;
using Reki.Infrastructure.ML.Classifier;
using Reki.Infrastructure.Vk;
using Reki.Infrastructure.Yandex;

namespace Reki.Api.Serialization;

[JsonSerializable(typeof(EnergyExchangeResponse))]
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
[JsonSerializable(typeof(NotificationChannelType))]
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
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(VkUserInfoResponse))]
[JsonSerializable(typeof(YandexUserInfoResponse))]
[JsonSerializable(typeof(PlayerId))]
[JsonSerializable(typeof(StreakId))]
[JsonSerializable(typeof(PostId))]
[JsonSerializable(typeof(CommentId))]
[JsonSerializable(typeof(Stream))]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(PlayerResponse))]
[JsonSerializable(typeof(PlayerStreakResponse))]
[JsonSerializable(typeof(ReferralResponse))]
[JsonSerializable(typeof(OnboardingRequest))]
[JsonSerializable(typeof(MergePlayersRequest))]
[JsonSerializable(typeof(CreatePostRequest))]
[JsonSerializable(typeof(LikePostRequest))]
[JsonSerializable(typeof(CreatePostResponse))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(GenerateReplyRequest))]
[JsonSerializable(typeof(ReplyResponse))]
[JsonSerializable(typeof(ReplyGenerationStatus))]
[JsonSerializable(typeof(ReplyGenerationLiveEvent))]
[JsonSerializable(typeof(SseItem<ReplyGenerationLiveEvent>))]
[JsonSerializable(typeof(DirectMessageRequest))]
[JsonSerializable(typeof(ForgetMemoryRequest))]
[JsonSerializable(typeof(StartChallengeRequest))]
[JsonSerializable(typeof(ReportRequest))]
[JsonSerializable(typeof(UpdateSettingsRequest))]
[JsonSerializable(typeof(WebPushSubscriptionRequest))]
[JsonSerializable(typeof(FeedItemResponse[]))]
[JsonSerializable(typeof(WorldPostsRequest))]
[JsonSerializable(typeof(PageResponse<FeedItemResponse>))]
[JsonSerializable(typeof(IReadOnlyList<FeedItemResponse>))]
[JsonSerializable(typeof(PostDetailResponse))]
[JsonSerializable(typeof(FriendResponse[]))]
[JsonSerializable(typeof(IReadOnlyList<FriendResponse>))]
[JsonSerializable(typeof(DmResponse))]
[JsonSerializable(typeof(SendDirectMessageResponse))]
[JsonSerializable(typeof(ChallengeResponse))]
[JsonSerializable(typeof(WorldResponse))]
[JsonSerializable(typeof(IReadOnlyList<WorldResponse>))]
[JsonSerializable(typeof(CreateWorldRequest))]
[JsonSerializable(typeof(PlusInterestRequest))]
[JsonSerializable(typeof(PlusInterestResponse))]
[JsonSerializable(typeof(AnalyticsBatchRequest))]
[JsonSerializable(typeof(ClientLogBatchRequest))]
[JsonSerializable(typeof(ClassifyRequest))]
[JsonSerializable(typeof(ClassifyResponse))]
[JsonSourceGenerationOptions(
    Converters =
    [
        typeof(JsonStringEnumConverter<AddressType>),
        typeof(CamelCaseEnumConverter<PostViralityStatus>),
        typeof(CamelCaseEnumConverter<WorldDrama>),
        typeof(CamelCaseEnumConverter<WorldDifficulty>),
        typeof(CamelCaseEnumConverter<FeedItemKind>),
        typeof(CamelCaseEnumConverter<DirectMessageAuthor>),
        typeof(CamelCaseEnumConverter<ReplyGenerationStatus>),
        typeof(CamelCaseEnumConverter<ClientLogLevel>),
        typeof(CamelCaseEnumConverter<DailyTaskType>),
        typeof(KebabCaseEnumConverter<LaunchSurface>),
    ],
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class ApiJsonSerializerContext : JsonSerializerContext;
