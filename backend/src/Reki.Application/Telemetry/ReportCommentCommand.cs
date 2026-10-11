using Mediator;
using Reki.Domain.Posts;

namespace Reki.Application.Telemetry;

public sealed record ReportCommentCommand(CommentId CommentId, string Reason) : IRequest;
