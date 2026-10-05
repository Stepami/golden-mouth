using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record ReportCommentCommand(long TelegramUserId, string CommentId, string Reason) : IRequest;
