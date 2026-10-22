using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Vogen;

namespace Reki.Infrastructure.Persistence;

[BsonSerializer<AuthorId>]
[BsonSerializer<CommentId>]
[BsonSerializer<EventId>]
[BsonSerializer<InvitationId>]
[BsonSerializer<PlayerId>]
[BsonSerializer<PostId>]
[BsonSerializer<SessionId>]
[BsonSerializer<SkillId>]
[BsonSerializer<StarId>]
[BsonSerializer<TelegramUserId>]
[BsonSerializer<WorldId>]
internal partial class BsonValueObjectConversions;
