using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Telemetry.ReportCommentCommand;

public sealed record ReportComment(CommentId CommentId, string Reason) : ICommand;
