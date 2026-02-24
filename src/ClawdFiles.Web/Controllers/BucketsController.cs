using System.IO.Compression;
using System.Security.Claims;
using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Interfaces;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/buckets")]
public class BucketsController(
    BucketService bucketService,
    BucketSummaryService summaryService,
    IFileHeaderRepository fileRepo,
    IFileStorage storage) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBucketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("name is required", "Provide a bucket name"));
        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));
        return Ok(await bucketService.CreateBucketAsync(request, ownerId.Value));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List()
    {
        var isAdmin = User.IsInRole("Admin");
        return Ok(await bucketService.ListBucketsAsync(GetCallerId(), isAdmin));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string id)
    {
        var result = await bucketService.GetBucketAsync(id);
        return result is null ? NotFound(new ErrorResponse("Bucket not found")) : Ok(result);
    }

    [HttpPatch("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBucketRequest request)
    {
        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));
        var result = await bucketService.UpdateBucketAsync(id, request, ownerId.Value, User.IsInRole("Admin"));
        return result is null ? NotFound(new ErrorResponse("Bucket not found or access denied")) : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        var ownerId = GetCallerId();
        if (ownerId is null) return Unauthorized(new ErrorResponse("Invalid authentication"));
        return await bucketService.DeleteBucketAsync(id, ownerId.Value, User.IsInRole("Admin"))
            ? Ok(new { message = "Bucket deleted" })
            : NotFound(new ErrorResponse("Bucket not found or access denied"));
    }

    [HttpGet("{id}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> Summary(string id)
    {
        var summary = await summaryService.GetSummaryAsync(id);
        return summary is null ? NotFound(new ErrorResponse("Bucket not found")) : Content(summary, "text/plain");
    }

    [HttpGet("{id}/zip")]
    [AllowAnonymous]
    public async Task<IActionResult> Zip(string id)
    {
        var bucket = await bucketService.GetBucketAsync(id);
        if (bucket is null) return NotFound(new ErrorResponse("Bucket not found"));
        var files = await fileRepo.ListByBucketAsync(id);
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                var fileStream = await storage.GetFileStreamAsync(id, file.Path);
                if (fileStream is not null) { await using (fileStream) { await fileStream.CopyToAsync(entryStream); } }
            }
        }
        ms.Position = 0;
        return File(ms, "application/zip", $"{id}.zip");
    }

    private Guid? GetCallerId()
    {
        var idClaim = User.FindFirst("api_key_id")?.Value;
        return idClaim is not null && Guid.TryParse(idClaim, out var id) ? id : null;
    }
}
