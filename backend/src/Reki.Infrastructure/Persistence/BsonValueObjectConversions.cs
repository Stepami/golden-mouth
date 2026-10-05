using Reki.Domain.Social;
using Reki.Domain.Worlds;
using Vogen;

namespace Reki.Infrastructure.Persistence;

[BsonSerializer<StarConversationId>]
[BsonSerializer<WorldRulesId>]
internal partial class BsonValueObjectConversions;
