using AutoMapper;
using Common.Models.DTOs;
using Common.Models.Entities;
using Common.Models.Requests;
using Services.Interfaces;
using Services.Interfaces.Aws;

namespace Services.Concrete;

public class PostService : IPostService
{
    private readonly IS3Service _is3Service;
    private readonly IDynamoDBService _dynamoDBService;
    private readonly IMapper _mapper;
    public PostService(IDynamoDBService dynamoDBService, IS3Service is3Service, IMapper mapper)
    {
        _dynamoDBService = dynamoDBService;
        _is3Service = is3Service;
        _mapper = mapper;
    }
    public async Task<PostDto> CreatePostAsync(CreatePostRequest request)
    {
        var s3Key = Guid.NewGuid();

        await _is3Service.UploadImageToS3Async(request.Image, s3Key);
        
        var post = _mapper.Map<PostEntity>(request);
        post.ImageId = s3Key;

        var createdPost = await _dynamoDBService.SavePostAsync(post);
        return _mapper.Map<PostDto>(createdPost);
    }

    public async Task<CommentDto> AddCommentAsync(AddCommentRequest request)
    {
        var commentToAdd = _mapper.Map<CommentEntity>(request);

        var createdComment = await _dynamoDBService.SaveCommentAsync(commentToAdd);
        
        return _mapper.Map<CommentDto>(createdComment);
    }

    public async Task DeleteCommentAsync(DeleteCommentRequest request)
    {
        await _dynamoDBService.DeleteCommentAsync(request.Id, request.PostId, request.CreatorId, request.CreatedAt);
    }

    public async Task<PaginatedPostDto> GetPostsAsync(string? cursor, int? limit)
    {
        var result = await _dynamoDBService.GetPostsAsync(cursor, limit);
        var posts = _mapper.Map<List<PostDto>>(result.Item1);
        return new PaginatedPostDto
        {
            Posts = posts,
            NextCursor = result.Item2
        };
    }
}