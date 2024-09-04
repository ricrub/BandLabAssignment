using Common.Models;
using Common.Models.Entities;

namespace Services.Interfaces.Aws;

public interface IDynamoDBService
{
    Task<PostEntity> SavePostAsync(PostEntity postEntity);
    Task<(List<PostEntity>, string)> GetPostsAsync(string? cursor, int? limit);
    Task<CommentEntity> SaveCommentAsync(CommentEntity commentEntity);
    Task DeleteCommentAsync(Guid commentId, Guid postId, Guid creatorId, DateTime createdAt);
}