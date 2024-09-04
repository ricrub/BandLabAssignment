namespace Common.Models.DTOs;

public class PaginatedPostDto
{
    public List<PostDto> Posts { get; set; }
    public string NextCursor { get; set; }    
}