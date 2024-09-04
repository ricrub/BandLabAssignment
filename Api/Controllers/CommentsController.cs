using Common.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly IPostService _postService;

    public CommentsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
    {
        return Ok(await _postService.AddCommentAsync(request));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentRequest request)
    {
        await _postService.DeleteCommentAsync(request);
        return NoContent();
    }
}