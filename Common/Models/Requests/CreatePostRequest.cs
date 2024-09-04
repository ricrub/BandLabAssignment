using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Common.Models.Requests;

public class CreatePostRequest
{
    [Required]
    public string Caption { get; set; }
    [Required]
    public Guid CreatorId { get; set; }
    [Required]
    public IFormFile Image { get; set; }
}