using Mediator;
using Reki.Application.Posts.Responses.ReplyGeneration;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GenerateReplyCommand;

public sealed record GenerateReply(
    PostId PostId,
    WorldId WorldId,
    AuthorId AuthorId,
    CommentId SourceCommentId,
    bool Additional) : IStreamCommand<ReplyGenerationLiveEvent>;
