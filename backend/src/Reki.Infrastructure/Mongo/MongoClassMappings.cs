using System.Collections.ObjectModel;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Reki.Domain.Bonuses;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Player.Streaks;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Vogen;

namespace Reki.Infrastructure.Mongo;

[BsonSerializer<Address>]
[BsonSerializer<AuthorId>]
[BsonSerializer<BonusId>]
[BsonSerializer<CommentId>]
[BsonSerializer<NotificationId>]
[BsonSerializer<PlayerId>]
[BsonSerializer<PostId>]
[BsonSerializer<SessionId>]
[BsonSerializer<SkillId>]
[BsonSerializer<StarId>]
[BsonSerializer<StreakId>]
[BsonSerializer<ExternalUserId>]
[BsonSerializer<WorldId>]
internal static partial class MongoClassMappings
{
    /// <summary>
    /// BSON mappings must be registered once before any infrastructure code uses them.
    /// </summary>
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<WorldDifficulty>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<DailyTaskType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<AddressType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new AddressBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new AuthorIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BonusIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new CommentIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new NotificationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new PlayerIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new PostIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new SessionIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new SkillIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new StarIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new StreakIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new ExternalUserIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new WorldIdBsonSerializer());

        RegisterBonuses();
        RegisterNotificationChannels();

        BsonClassMap.TryRegisterClassMap<PlayerChallenge>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(item => item.Values)
                .SetSerializer(
                    new DictionaryInterfaceImplementerSerializer<
                        Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>>,
                        WorldId,
                        Dictionary<DailyTaskType, DailyTaskValues>>(
                        DictionaryRepresentation.ArrayOfDocuments));
        });

        RegisterWithMongoId<PlayerProfile, PlayerId>(item => item.Id, true, classMap =>
        {
            classMap.MapMember(item => item.ExternalUserIds);
            classMap.MapMember(item => item.NotificationChannels)
                .SetDefaultValue(ReadOnlyDictionary<NotificationChannelType, PlayerNotificationChannel>.Empty)
                .SetSerializer(new ImpliedImplementationInterfaceSerializer<
                    IReadOnlyDictionary<NotificationChannelType, PlayerNotificationChannel>,
                    ReadOnlyDictionary<NotificationChannelType, PlayerNotificationChannel>>(
                    new ReadOnlyDictionaryInterfaceImplementerSerializer<
                        ReadOnlyDictionary<NotificationChannelType, PlayerNotificationChannel>,
                        NotificationChannelType,
                        PlayerNotificationChannel>(
                        DictionaryRepresentation.Document,
                        new EnumSerializer<NotificationChannelType>(BsonType.String),
                        BsonSerializer.LookupSerializer<PlayerNotificationChannel>())));
            classMap.MapMember(item => item.Skills);
            classMap.MapMember(item => item.Version).SetDefaultValue(0);
            classMap.MapMember(item => item.HitOwnWorldMilestone).SetDefaultValue(false);
            classMap.MapMember(item => item.LastSeenAt).SetDefaultValue(default(DateTimeOffset));
            classMap.MapMember(item => item.PlusInterestClaimed).SetDefaultValue(false);
            classMap.MapMember(item => item.PausedAtUtc).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.HateDisabledUntilUtc).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.PlusNotifyRequested).SetDefaultValue(false);
            classMap.MapMember(item => item.BirthYear).SetDefaultValue(0);
            classMap.MapMember(item => item.DailyChallenge).SetDefaultValue((object?)null);
            classMap.MapCreator(item => new PlayerProfile(
                    item.Id,
                    item.ExternalUserIds,
                    item.Address,
                    item.Wallet,
                    item.Skills,
                    item.BirthYear,
                    item.Settings,
                    item.NotificationChannels,
                    item.HitOwnWorldMilestone,
                    item.LastSeenAt,
                    item.PlusInterestClaimed,
                    item.PausedAtUtc,
                    item.HateDisabledUntilUtc,
                    item.Version,
                    item.PlusNotifyRequested,
                    item.DailyChallenge));
        });

        RegisterWithMongoId<PlayerStreak, StreakId>(item => item.Id, configure: classMap =>
        {
            classMap.MapCreator(item => new PlayerStreak(item.Id, item.PlayerId, item.StartedAt, item.Count, item.IsActive));
        });

        if (!BsonClassMap.IsClassMapRegistered(typeof(PlayerWallet)))
        {
            BsonClassMap.RegisterClassMap<PlayerWallet>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
                classMap.MapCreator(item => new PlayerWallet(
                    item.Energy,
                    item.Crystals,
                    item.EnergyUpdatedAtUtc));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(WorldPlayer)))
        {
            BsonClassMap.RegisterClassMap<WorldPlayer>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
                classMap.MapMember(item => item.Achievements).SetDefaultValue(Array.Empty<AchievementProgress>());
                classMap.MapMember(item => item.Voice).SetDefaultValue("default");
                classMap.MapMember(item => item.Drama).SetDefaultValue(WorldDrama.Mid);
                classMap.MapMember(item => item.Name).SetDefaultValue(string.Empty);
                classMap.MapMember(item => item.Role).SetDefaultValue(string.Empty);
                classMap.MapMember(item => item.Avatar).SetDefaultValue(string.Empty);
                classMap.MapMember(item => item.Subscribers).SetDefaultValue(0L);
                classMap.MapCreator(item => new WorldPlayer(
                    item.PlayerId,
                    item.Voice,
                    item.Drama,
                    item.Name,
                    item.Role,
                    item.Avatar,
                    item.Subscribers,
                    item.Achievements));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(WorldStar)))
        {
            BsonClassMap.RegisterClassMap<WorldStar>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(GameComment)))
        {
            BsonClassMap.RegisterClassMap<GameComment>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapMember(item => item.Reactions).SetDefaultValue(Array.Empty<CommentReaction>());
                classMap.MapMember(item => item.ParentCommentId).SetDefaultValue((object?)null);
                classMap.MapMember(item => item.RequestId).SetDefaultValue((object?)null);
                classMap.MapMember(item => item.AdditionalReplierId).SetDefaultValue((object?)null);
                classMap.MapCreator(item => new GameComment(
                    item.Id,
                    item.ParentCommentId,
                    item.RequestId,
                    item.AuthorId,
                    item.Text,
                    item.Hidden,
                    item.Reactions,
                    item.AdditionalReplierId));
            });
        }

        RegisterWithMongoId<GamePost, PostId>(item => item.Id, true, classMap =>
        {
            classMap.MapMember(item => item.OwnerId).SetDefaultValue(PlayerId.Empty);
            classMap.MapMember(item => item.LikedBy).SetDefaultValue(Array.Empty<AuthorId>());
            classMap.MapMember(item => item.Comments).SetDefaultValue(Array.Empty<GameComment>());
            classMap.MapMember(item => item.Likes).SetDefaultValue(0L);
            classMap.MapMember(item => item.Virality).SetDefaultValue(PostViralityStatus.None);
            classMap.MapMember(item => item.Version).SetDefaultValue(0);
            classMap.MapMember(item => item.PredictedAuthorId).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.FlopReason).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.RepostId).SetDefaultValue((object?)null);
            classMap.MapCreator(item => new GamePost(
                    item.Id,
                    item.AuthorId,
                    item.WorldId,
                    item.OwnerId,
                    item.Text,
                    item.Views,
                    item.Likes,
                    item.LikedBy,
                    item.Comments,
                    item.CreatedAtUtc,
                    item.SubscriberDelta,
                    item.Quality,
                    item.ReachRatio,
                    item.Virality,
                    item.Version,
                    item.PredictedAuthorId,
                    item.FlopReason,
                    item.RepostId));
        });
        RegisterWithMongoId<World, WorldId>(item => item.Id, configure: classMap =>
        {
            classMap.MapMember(item => item.Roles).SetDefaultValue(Array.Empty<string>());
            classMap.MapMember(item => item.Voices).SetDefaultValue(Array.Empty<string>());
            classMap.MapMember(item => item.PostIdeas).SetDefaultValue(Array.Empty<string>());
            classMap.MapMember(item => item.DemoPosts).SetDefaultValue(Array.Empty<string>());
            classMap.MapMember(item => item.Rules).SetDefaultValue(Array.Empty<WorldRule>());
            classMap.MapMember(item => item.Stars).SetDefaultValue(Array.Empty<WorldStar>());
            classMap.MapMember(item => item.Version).SetDefaultValue(0);
            classMap.MapMember(item => item.Difficulty).SetDefaultValue(WorldDifficulty.Normal);
            classMap.MapMember(item => item.IsArchived).SetDefaultValue(false);
            classMap.MapMember(item => item.TemplateId).SetDefaultValue((object?)null);
            classMap.MapCreator(item => new World(
                    item.Id,
                    item.Interface,
                    item.Player,
                    item.TemplateId,
                    item.Difficulty,
                    item.IsArchived,
                    item.Roles,
                    item.Voices,
                    item.PostIdeas,
                    item.DemoPosts,
                    item.Rules,
                    item.Stars,
                    item.Version));
        });
        RegisterWithMongoId<Notification, NotificationId>(
            item => item.Id,
            configure: classMap =>
            {
                classMap.MapMember(item => item.ChannelType).SetDefaultValue(default(NotificationChannelType));
                classMap.MapMember(item => item.Kind).SetDefaultValue(NotificationKind.Regular);
                classMap.MapMember(item => item.SendAt).SetSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));
                classMap.MapMember(item => item.Version).SetDefaultValue(0);
                classMap.MapCreator(item => new Notification(
                    item.Id,
                    item.PlayerId,
                    item.ChannelType,
                    item.Status,
                    item.SendAt,
                    item.Content,
                    item.Version,
                    item.Kind));
            });
    }

    private static void RegisterNotificationChannels()
    {
        BsonClassMap.TryRegisterClassMap<PlayerNotificationChannel>(classMap => classMap.AutoMap());
        BsonClassMap.TryRegisterClassMap<TelegramNotificationChannel>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(item => item.ChatId);
            classMap.MapCreator(item => new TelegramNotificationChannel(item.ChatId));
        });
        BsonClassMap.TryRegisterClassMap<WebPushNotificationChannel>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(item => item.Subscriptions);
            classMap.MapCreator(item => new WebPushNotificationChannel(item.Subscriptions));
        });
        BsonClassMap.TryRegisterClassMap<WebPushSubscription>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapCreator(item => new WebPushSubscription(item.Endpoint, item.P256dh, item.Auth));
        });
    }

    private static void RegisterBonuses()
    {
        RegisterWithMongoId<Bonus, BonusId>(bonus => bonus.Id, configure: classMap =>
        {
            classMap.MapMember(bonus => bonus.PlayerId);
            classMap.SetIsRootClass(true);
        });
        BsonClassMap.TryRegisterClassMap<CrystalsBonus>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(bonus => bonus.Amount);
            classMap.MapCreator(bonus => new CrystalsBonus(bonus.Id, bonus.PlayerId, bonus.Amount, bonus.IsClaimed));
        });
        BsonClassMap.TryRegisterClassMap<ViralCardBonus>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(bonus => bonus.Multiplier);
            classMap.MapCreator(bonus =>
                new ViralCardBonus(bonus.Id, bonus.PlayerId, bonus.Multiplier, bonus.IsClaimed));
        });
        BsonClassMap.TryRegisterClassMap<FlopGuardBonus>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapMember(bonus => bonus.Adder);
            classMap.MapCreator(bonus => new FlopGuardBonus(bonus.Id, bonus.PlayerId, bonus.Adder, bonus.IsClaimed));
        });
    }

    private static void RegisterWithMongoId<TDocument, TId>(
        Expression<Func<TDocument, TId>> idExpression,
        bool ignoreExtraElements = false,
        Action<BsonClassMap<TDocument>>? configure = null)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(TDocument)))
        {
            return;
        }

        BsonClassMap.RegisterClassMap<TDocument>(classMap =>
        {
            classMap.AutoMap();
            classMap.MapIdMember(idExpression);
            classMap.SetIgnoreExtraElements(ignoreExtraElements);
            configure?.Invoke(classMap);
        });
    }
}
