using System.ComponentModel.DataAnnotations;

namespace Common.Models.Requests;

public class AddCommentRequest
{
    [Required]
    public Guid PostId { get; set; }
    [Required]
    public string Content { get; set; }
    [Required]
    public Guid CreatorId { get; set; }
}
