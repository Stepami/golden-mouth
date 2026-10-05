using Mediator;

namespace Reki.Application.Telemetry;

public sealed record ReportCommentCommand(long TelegramUserId, string CommentId, string Reason) : IRequest;
