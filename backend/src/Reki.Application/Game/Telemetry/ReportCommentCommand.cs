using Mediator;

namespace Reki.Application.Game.Telemetry;

public sealed record ReportCommentCommand(long TelegramUserId, string CommentId, string Reason) : IRequest;
