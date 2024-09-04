namespace Common.Models.DTOs;

public class PostDto
{
    public Guid Id { get; set; }
    public string Caption { get; set; }
    public Guid ImageId { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentCount { get; set; }

    public List<CommentDto> Comments { get; set; }
}