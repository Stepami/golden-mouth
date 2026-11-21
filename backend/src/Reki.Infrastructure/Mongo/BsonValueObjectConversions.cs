using Reki.Domain.Bonuses;
using Reki.Domain.Invites;
using Reki.Domain.Notifications;
using Reki.Domain.Player;
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
[BsonSerializer<InvitationId>]
[BsonSerializer<NotificationId>]
[BsonSerializer<PlayerId>]
[BsonSerializer<PostId>]
[BsonSerializer<SessionId>]
[BsonSerializer<SkillId>]
[BsonSerializer<StarId>]
[BsonSerializer<StreakId>]
[BsonSerializer<ExternalUserId>]
[BsonSerializer<WorldId>]
internal partial class BsonValueObjectConversions;
