using System.Text.Json.Serialization;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Jobs;

[JsonSerializable(typeof(NotificationChannelType))]
internal sealed partial class NotificationJobsJsonContext : JsonSerializerContext;
