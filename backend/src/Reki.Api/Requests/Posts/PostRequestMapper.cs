using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GenerateReplyCommand;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Posts;

[Mapper]
internal static partial class PostRequestMapper
{
    [MapProperty(nameof(ReplyRequest.CommentId), nameof(AddReply.TargetCommentId))]
    public static partial AddReply ToAddReply(this ReplyRequest source, PostId postId, WorldId worldId);

    public static partial GenerateReply ToGenerateReply(this GenerateReplyRequest source, PostId postId, WorldId worldId);

    public static partial WorldPostsQuery ToQuery(this WorldPostsRequest source, bool feed = false, bool liked = false);
}
