using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Telemetry;

public sealed record ReportCommentCommand(TelegramUserId TelegramUserId, CommentId CommentId, string Reason) : IRequest;
