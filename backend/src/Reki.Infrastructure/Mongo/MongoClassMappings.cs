using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Reki.Domain.Invites;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Player.Streaks;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo;

internal static class MongoClassMappings
{
    /// <summary>
    /// BSON mappings must be registered once before any infrastructure code uses them.
    /// </summary>
    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<WorldDifficulty>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<DailyTaskType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new EnumSerializer<AddressType>(BsonType.String));
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.AddressBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.AuthorIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.CommentIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.InvitationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(new BsonValueObjectConversions.NotificationIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.PlayerIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.PostIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.SessionIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.SkillIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.StarIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.StreakIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.ExternalUserIdBsonSerializer());
        BsonSerializer.TryRegisterSerializer(
            new BsonValueObjectConversions.WorldIdBsonSerializer());

        var taskValuesSerializer = new DictionaryInterfaceImplementerSerializer<Dictionary<DailyTaskType, DailyTaskValues>, DailyTaskType, DailyTaskValues>(DictionaryRepresentation.ArrayOfDocuments);

        if (!BsonClassMap.IsClassMapRegistered(typeof(PlayerChallenge)))
        {
            BsonClassMap.RegisterClassMap<PlayerChallenge>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
                classMap.MapMember(item => item.Completion).SetSerializer(
                    new DictionaryInterfaceImplementerSerializer<Dictionary<DailyTaskType, bool>, DailyTaskType, bool>(DictionaryRepresentation.ArrayOfDocuments));
                classMap.MapMember(item => item.Values).SetSerializer(
                    new DictionaryInterfaceImplementerSerializer<Dictionary<WorldId, Dictionary<DailyTaskType, DailyTaskValues>>, WorldId, Dictionary<DailyTaskType, DailyTaskValues>>(
                        DictionaryRepresentation.ArrayOfDocuments,
                        BsonSerializer.LookupSerializer<WorldId>(),
                        taskValuesSerializer));
                classMap.MapCreator(item => new PlayerChallenge(item.Day, item.Completion, item.Values));
            });
        }

        RegisterWithMongoId<PlayerProfile, PlayerId>(item => item.Id, true, classMap =>
        {
            classMap.MapMember(item => item.ExternalUserIds);
            classMap.MapMember(item => item.NotificationChannels).SetDefaultValue(Array.Empty<PlayerNotificationChannel>());
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

        if (!BsonClassMap.IsClassMapRegistered(typeof(PlayerNotificationChannel)))
        {
            BsonClassMap.RegisterClassMap<PlayerNotificationChannel>(classMap =>
            {
                classMap.AutoMap();
                classMap.MapMember(item => item.Type);
                classMap.MapMember(item => item.Properties);
                classMap.MapCreator(item => new PlayerNotificationChannel(item.Type, item.Properties));
            });
        }

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
                    item.Viral,
                    item.Flop,
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
        RegisterWithMongoId<Invitation, InvitationId>(item => item.Id, configure: classMap =>
        {
            classMap.MapMember(item => item.InviterBonusUsedAtUtc).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.InviteeBonusUsedAtUtc).SetDefaultValue((object?)null);
            classMap.MapMember(item => item.Version).SetDefaultValue(0);
            classMap.MapCreator(item => new Invitation(
                    item.Id,
                    item.InviterId,
                    item.InviteeId,
                    item.CreatedAtUtc,
                    item.InviterBonusUsed,
                    item.InviteeBonusUsed,
                    item.BonusAmount,
                    item.InviterBonusUsedAtUtc,
                    item.InviteeBonusUsedAtUtc,
                    item.Version));
        });
        RegisterWithMongoId<Notification, NotificationId>(
            item => item.Id,
            configure: classMap =>
            {
                classMap.MapMember(item => item.SendAt).SetSerializer(new DateTimeOffsetSerializer(BsonType.DateTime));
                classMap.MapMember(item => item.Version).SetDefaultValue(0);
                classMap.MapCreator(item => new Notification(
                    item.Id,
                    item.PlayerId,
                    item.Status,
                    item.SendAt,
                    item.Content,
                    item.Version));
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
