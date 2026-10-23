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
    public static GeneratePublication ToGeneratePublication(
        this StartPublicationRequest source,
        WorldId worldId) =>
        MapPublication(source, worldId);

    public static AddReply ToAddReply(this ReplyRequest source, string postId, WorldId worldId) =>
        MapReply(source, PostId.From(postId), worldId);

    public static WorldPostsQuery ToQuery(this WorldPostsRequest source, bool feed = false) =>
        MapQuery(source, feed);

    [MapProperty(nameof(StartPublicationRequest.PublicationId), nameof(GeneratePublication.Id))]
    private static partial GeneratePublication MapPublication(StartPublicationRequest source, WorldId worldId);

    [MapProperty(nameof(ReplyRequest.CommentId), nameof(AddReply.TargetCommentId))]
    private static partial AddReply MapReply(ReplyRequest source, PostId postId, WorldId worldId);

    private static partial WorldPostsQuery MapQuery(WorldPostsRequest source, bool feed);

    private static PostId? MapPostId(Guid? source) => source is { } value ? PostId.From(value) : null;

    private static CommentId? MapCommentId(string? source) => source is null ? null : CommentId.From(source);
}
