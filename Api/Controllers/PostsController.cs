using Common.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromForm] CreatePostRequest request)
    {
        return Ok(await _postService.CreatePostAsync(request));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPosts([FromQuery] string? cursor, [FromQuery] int? limit)
    {
        return Ok(await _postService.GetPostsAsync(cursor, limit));
    }
}