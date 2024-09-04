using AutoMapper;
using Common.Models.DTOs;
using Common.Models.Entities;
using Common.Models.Requests;

namespace Api.Helpers;

public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<CreatePostRequest, PostEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<PostEntity, PostDto>()
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => MapComments(src)));
        
        CreateMap<AddCommentRequest, CommentEntity>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<CommentEntity, CommentDto>();
    }
    
    private List<CommentDto> MapComments(PostEntity postEntity)
    {
        var comments = new List<CommentDto>();

        if (postEntity.LastComment != null)
        {
            comments.Add(new CommentDto
            {
                Id = postEntity.LastComment.Id,
                PostId = postEntity.LastComment.PostId,
                Content = postEntity.LastComment.Content,
                CreatorId = postEntity.LastComment.Creator,
                CreatedAt = postEntity.LastComment.CreatedAt.ToUniversalTime()
            });
        }

        if (postEntity.SecondLastComment != null)
        {
            comments.Add(new CommentDto
            {
                Id = postEntity.SecondLastComment.Id,
                PostId = postEntity.SecondLastComment.PostId,
                Content = postEntity.SecondLastComment.Content,
                CreatorId = postEntity.SecondLastComment.Creator,
                CreatedAt = postEntity.SecondLastComment.CreatedAt.ToUniversalTime()
            });
        }

        return comments;
    }
}