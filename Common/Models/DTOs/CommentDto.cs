namespace Common.Models.DTOs;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public string Content { get; set; }
    public Guid CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
}