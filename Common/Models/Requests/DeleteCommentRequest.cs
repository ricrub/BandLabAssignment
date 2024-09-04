using System.ComponentModel.DataAnnotations;

namespace Common.Models.Requests;

public class DeleteCommentRequest
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid PostId { get; set; }
    [Required]
    public Guid CreatorId { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
}