using Reki.Domain.Events;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Vogen;

namespace Reki.Infrastructure.Mongo;

[BsonSerializer<Address>]
[BsonSerializer<AuthorId>]
[BsonSerializer<CommentId>]
[BsonSerializer<RekiEventId>]
[BsonSerializer<InvitationId>]
[BsonSerializer<PlayerId>]
[BsonSerializer<PostId>]
[BsonSerializer<SessionId>]
[BsonSerializer<SkillId>]
[BsonSerializer<StarId>]
[BsonSerializer<ExternalUserId>]
[BsonSerializer<WorldId>]
internal partial class BsonValueObjectConversions;
