using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Api.Requests.Posts;

[Mapper]
internal static partial class PostRequestMapper
{
    [MapProperty(nameof(StartPublicationRequest.PublicationId), nameof(GeneratePublication.Id))]
    public static partial GeneratePublication ToGeneratePublication(this StartPublicationRequest source, WorldId worldId);

    [MapProperty(nameof(ReplyRequest.CommentId), nameof(AddReply.TargetCommentId))]
    public static partial AddReply ToAddReply(this ReplyRequest source, PostId postId, WorldId worldId);

    public static partial WorldPostsQuery ToQuery(this WorldPostsRequest source, bool feed = false);
}
