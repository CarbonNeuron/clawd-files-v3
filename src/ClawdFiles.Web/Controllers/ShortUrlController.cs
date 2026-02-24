using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class ShortUrlController(FileService fileService) : ControllerBase
{
    [HttpGet("s/{shortCode}")]
    public new async Task<IActionResult> Redirect(string shortCode)
    {
        var header = await fileService.ResolveShortCodeAsync(shortCode);
        if (header is null) return NotFound(new ErrorResponse("Short URL not found"));
        return RedirectPreserveMethod($"/raw/{header.BucketId}/{header.Path}");
    }
}
