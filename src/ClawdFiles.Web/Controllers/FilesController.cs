using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/buckets/{bucketId}")]
public class FilesController(FileService fileService) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> Upload(string bucketId)
    {
        if (!Request.HasFormContentType)
            return BadRequest(new ErrorResponse("Expected multipart/form-data"));
        var form = await Request.ReadFormAsync();
        var uploaded = new List<FileHeaderResponse>();
        foreach (var file in form.Files)
        {
            var fileName = !string.IsNullOrEmpty(file.FileName) ? file.FileName : file.Name;
            if (!ContentTypeProvider.TryGetContentType(fileName, out var contentType))
                contentType = file.ContentType ?? "application/octet-stream";
            using var stream = file.OpenReadStream();
            var result = await fileService.UploadFileAsync(bucketId, fileName, contentType, stream);
            if (result is null) return NotFound(new ErrorResponse("Bucket not found", $"No bucket with id '{bucketId}'"));
            uploaded.Add(result);
        }
        return Ok(new UploadResponse(uploaded));
    }

    [HttpDelete("files")]
    [Authorize]
    public async Task<IActionResult> Delete(string bucketId, [FromBody] DeleteFileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path))
            return BadRequest(new ErrorResponse("path is required"));
        return await fileService.DeleteFileAsync(bucketId, request.Path)
            ? Ok(new { message = "File deleted" })
            : NotFound(new ErrorResponse("File not found"));
    }
}
