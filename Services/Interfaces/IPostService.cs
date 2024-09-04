using Common.Models.DTOs;
using Common.Models.Requests;

namespace Services.Interfaces;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(CreatePostRequest request);
    Task<PaginatedPostDto> GetPostsAsync(string? cursor, int? limit);
    Task<CommentDto> AddCommentAsync(AddCommentRequest request);
    Task DeleteCommentAsync(DeleteCommentRequest request);
}