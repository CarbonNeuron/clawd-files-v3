using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class RawController(IFileStorage storage, IFileHeaderRepository fileRepo) : ControllerBase
{
    [HttpGet("raw/{bucketId}/{**filePath}")]
    public async Task<IActionResult> GetRaw(string bucketId, string filePath)
    {
        var header = await fileRepo.FindByBucketAndPathAsync(bucketId, filePath);
        if (header is null) return NotFound(new ErrorResponse("File not found"));
        var stream = await storage.GetFileStreamAsync(bucketId, filePath);
        if (stream is null) return NotFound(new ErrorResponse("File not found on disk"));
        return File(stream, header.ContentType, enableRangeProcessing: true);
    }
}
